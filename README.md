![](icon.png)

# deploy
`deploy` allows developers to write complex deployment logic in JavaScript to deploy to SSH-enabled servers.

## Features
- A full V8 JavaScript engine, provided by [ClearScript](https://github.com/Microsoft/ClearScript), allows for deployment logic of any complexity.
- Straight-forward deployment logic using hashlists - only deploy objects to the server that have actually changed.
- Start/stop `systemctl` services and modify file permissions.
- Execute any SSH command in your JavaScript.

## Installing
To install `deploy`, either clone this repository and build from source, or download from our [releases]().  Add the containing folder for `deploy` to your `PATH`, and then simply use the command `deploy` in any folder that contains a `deploy.js` file.

## Example
The following is a simple example of a `deploy.js` file that stops a running service, uploads any changed files in a folder, and then restarts the service.  It retrieves the password for the key file from the command line args, so we can run in the prompt like so: `deploy superDuperGoodPassword`.

```javascript
if (local.args.length != 1) { throw "Expecting key file password as first argument."; }

let hosts = [ "api.myproject.com" ];

let getPortForHost = h => 22;
let getUsernameForHost = h => "subroot";
let getKeyfileForHost = h => "../ssh/private";
let getKeyfilePasswordForKeyfile = k => local.args[0];

let deploy = (h, ssh) => {
    ssh.setHashlistPath("/home/subroot/.hashlist");
    ssh.stopService("apid");

    let targets = local.filePairCollection("/home/subroot", "./bin", true);
    for (let x of targets) {
        ssh.uploadIfRequired(x);
    }

    ssh.startService("apid");
};
```

## Documentation
A complete listing of available functions and definitions is available below.

### Optional Definitions

#### `useVerboseMessages`
Define `useVerboseMessages` as a global variable to toggle automatic output from `deploy`.  If you set `useVerboseMessages` to `false`, the only output that will display for each host is output you write explicitly yourself using `local.write`.
```javascript
// Disable verbose output.
let useVerboseMessages = false;
```

### Required Definitions
The following variables and functions are required to be define for `deploy` to function.

#### `hosts`
`hosts` is an array of string values that represent each host to connect to.  Each element in this array is passed as a parameter to the following functions to determine connection information for that host.
```javascript
// Deploy to two different servers.
let hosts = [ "api1.example.com", "api2.example.com" ];
```

#### `getPortForHost`
`getPortForHost` is a function that takes a hostname as its only parameter.  It should return the port number to connect on for the given host.
```javascript
// Connect on the default port for all hosts.
let getPortForHost = h => 22;
```
```javascript
// Connect on the default port, except for an external server.
let getPortForHost = h => (h == "external-server.example.com" ? 2202 : 22);
```

#### `getUsernameForHost`
`getUsernameForHost` is a function that takes a hostname as its only parameter.  It should return the username to login as on the given host.
```javascript
// I like to use subroot as the name of my default sudo user.
let getUsernameForHost = h => "subroot";
```

#### `getKeyfileForHost`
`getKeyfileForHost` is a function that takes a hostname as its only parameter.  It should return the path to an OpenSSH encrypted key file, like the one shown below.  Note that `deploy` does not support password login, only keyfile.
```
-----BEGIN RSA PRIVATE KEY-----
Proc-Type: 4,ENCRYPTED
DEK-Info: DES-EDE3-CBC,1BD2463C718A7899

CEWsZDCc0Ym8V6k3i1VG6dEYRW5d51gVcC9IeiK6sOO1eLhOq/Uk02I2vqtwXM1k
qJdAquNpz0Ffz7m2Lidbv5MdE7RTdRmcdUdTObCZt4whlJeXl0Iz7VMhN3fimE9n
+Jc+Ktstpijr9PNf08l5BFHYqtBx8WevIyZy30J4w...
-----END RSA PRIVATE KEY-----
```
```javascript
let getKeyfileForHost = h => "../ssh/private-" + h;
```

#### `getKeyfilePasswordForKeyfile`
`getKeyfilePasswordForKeyfile` is a function that takes a **keyfile path** as its only parameter.  It should return the password for this keyfile.  It is **highly recommended** that you use the below interaction by default, however, you can hardcode the password or read from an external file using `local.readFile`.
```javascript
// Require the keyfile password to be passed as an argument to deploy.
let getKeyfilePasswordForKeyfile = k => local.args[0];
```

#### `deploy`
`deploy` is the main event.  This function is called for each host in `hosts`, passing a different `ssh` object each time.  The `ssh` object allows for SSH interaction with the remote server.  You can assume that the connection has been successfully established before `deploy` is called for each respective server.  You can interact with the local environment by using the `local` global object.
```javascript
// See the example above for an example of deploy.
let deploy = (host, ssh) => { ... }
```

### Local Environment
All of the functions and variables below are defined on the global object `local`.

#### `args`
`local.args` is the array of command line arguments that have been passed to `deploy`.

#### `write`
Writes a line of output to the console.  See `useVerboseMessages` if you only want output from this function to appear.
```javascript
local.write("Hello, World!");
```

#### `readFile`
Reads the contents of the given filename and returns them as a string.
```javascript
let strContents = local.readFile("list-of-cat-names.txt");
```

#### `writeFile`
Overwrites the contents of an existing file, or creates it if it doesn't exist.
```javascript
local.writeFile("list-of-cat-names.txt", "Helen\nSusan\nRobert");
```

#### `appendFile`
Appends text to the end of an existing file.
```javascript
local.appendFile("list-of-cat-names.txt", "\nNathaniel");
```

#### `deleteFile`
Deletes the given file.
```javascript
local.write("All my cat names are bad :(");
local.deleteFile("list-of-cat-names.txt");
```

#### `fileExists`
Returns true if the given filename exists on the local machine.
```javascript
// Cat names, where art thou?
local.fileExists("list-of-cat-names.txt"); // false
```

#### `filePair`
Takes two string parameters, the first is the remote path and the second is the local path.  Returns a file pair.
```javascript
let filePair = local.filePair("/home/subroot/test.txt", "./test.txt");
// {"remotePath":"/home/subroot/test.txt","localPath":"./test.txt"}
```

#### `filePairCollection`
Defined as `local.filePairCollection(remoteDir, localDir, recursive)`.  `filePairCollection` returns an array of file pairs that copy each file in `localDir` to `remoteDir`.  If `recursive` is true, subdirectories and their files are added also.

Consider the following local directory structure:
- ./bin
    - linux_amd64
        - example-api
    - notes.log

The following code:
```javascript
let filePairs = local.filePairCollection("/home/subroot", "./bin", true);
```
Will produce a value for `filePairs` as:
```json
[
    { "remotePath": "/home/subroot/linux_amd64/example-api", "localPath": "./bin/linux_amd64/example-api" },
    { "remotePath": "/home/subroot/notes.log", "localPath": "./bin/notes.log" }
]
```

### Remote Environment
All of the functions and variables below are defined on the instances of `ssh` that are passed to `deploy`.

#### The Hashlist
The hashlist is a file that is stored on each remote server that contains a collection of hashes for the files we deployed in our last deployment.  We use the hashlist to determine if a local copy of a file is different from the copy on our remote server (and thus, presumably, newer).  The hashlist is automatically returned to the server after each deployment.

The hashlist is only used if you use functions that require it.  You can avoid using it by simply avoiding any functions that use the hashlist (this includes `uploadIfRequired`).

#### `setHashlistPath`
Sets the remote location for the hashlist.
```javascript
// Store the hashlist in the users home directory.
ssh.setHashlistPath("/home/subroot/.hashlist");
```

#### `updateHashlist`
Updates the hashlist for the given filepair:
```javascript
let filePair = local.filePair("/home/subroot/apid", "./bin/linux_amd64/apid");
ssh.updateHashlist(filePair);
```
The hashlist now associates the SHA256 hash of `"./bin/linux_amd64/apid"` with `"/home/subroot/apid"`.

#### `matchesHashlist`
Checks if the SHA256 hash of the local file matches the entry in the hashlist for the remote file.  For example:
```javascript
let filePair = local.filePair("/home/subroot/apid", "./bin/linux_amd64/apid");
ssh.updateHashlist(filePair);
ssh.matchesHashlist(filePair); // true, since we just updated it.

local.writeFile("./bin/linux_amd64/apid", "Hello, World!");
ssh.matchesHashlist(filePair); // false, we changed the local file.
```

#### `purgeHashlist`
`purgeHashlist` is used to ensure you aren't leaving artifacts on the server that are no longer part of your deployment.  `purgeHashlist` takes an array of file pairs and purges both the server and the hashlist of any files that are not in the array.
```javascript
let deploymentFiles = [
    local.filePair("/home/subroot/.conf", "./conf/production.conf"),
    local.filePair("/home/subroot/service", "./bin/service")
];

// Deletes any files on the server that are in the hashlist but not deploymentFiles.
ssh.purgeHashlist(deploymentFiles);
```

#### `uploadFile`
`uploadFile` has no interaction with the hashlist.  It simply uploads the local file in a file pair to the remote path.
```javascript
let filePair = local.filePair("/home/subroot/.conf", "./conf/production.conf");
ssh.uploadFile(filePair);
```

#### `downloadFile`
The exact same as `uploadFile`, except the remote file is downloaded to the local path, overriding if necessary.

#### `deleteFile`
Takes only a remote path parameter as a string, attempts to delete the given file on the remote server:
```javascript
ssh.deleteFile("top-secret-stuff.txt");
```

#### `setFilePermissions`
Sets the file permissions for the remote file.  Takes three parameters.  The first is the file, the second is an octal representation of the file permissions e.g. `0700`, and the third is whether or not to use `sudo` to execute the command.
```javascript
ssh.setFilePermissions("top-secret-stuff.txt", "0600", true);
```

#### `uploadIfRequired`
This is pretty much the core function.  It is a direct convenience function for the following logic:
```javascript
if (!ssh.matchesHashlist(filePair)) { // The local copy is newer.
    ssh.uploadFile(filePair);
    ssh.updateHashlist(filePair);
}
```

#### `stopService`
Stops the given `systemctl` service.
```javascript
ssh.stopService("apid"); // > sudo systemctl stop apid
```

#### `startService`
Starts the given `systemctl` service.
```javascript
ssh.startService("apid"); // > sudo systemctl start apid
```

#### `executeCustomCommand`
Executes a raw command on the server.  This function has not been tested thoroughly.  It waits for output like "~$ " or "~# " before returning.
```javascript
ssh.executeCustomCommand("sudo systemctl enable apid");
```
