# SshSync : Directory synchronisation via SSH #


## Description:- ##
> A command line applications that allows intelligent Secure FTP transmissions. SshSync only support pull type transfers, but it allows
> use of a Private Key to ensure that authentication  is secure. A text file that contains a list of files always
> processed is used to check that only 'new' files are retrieved.

## Features:- ##
> - SFTP Pull over SSH
> - Fully configurable input parameter, including port number
> - Password or public key authentication are the only authentication type allowed
> - Test mode enables diagnostics without actually pulling over any files
> - .Net 2.0 managed code allows easy integration into other projects

## Requirements :- ##
.Net 2.0 Framework

## How to Run :- ##
To get help for this application, from a command prompt, run
```
SSHSync /?
```

example of normal use :-

```
SSHSync.exe /S:localhost /PN:22  /C:"C:\Incoming\FileCatalog.dat" /K:"C:\id_dsa" /U:sftpuser /R:./ /D:C:\incoming /W:*.* /CR:10 /debug /test
```

where
C:\Incoming\FileCatalog.dat is the file use to catalog previous retrievals. SSH will
create this file if it doesn't exist

C:\id\_dsa is the location of the private key being used for authentication

sftpuser is the user account

C:\incoming is the destination local folder

You'll get the idea, use the /test mode when setting up the parameters. It means that SSHSync won't actually transfer any data, just check settings, connections, file permissions and authentication.

Have fun!


---

# Thanks #
Many thanks to SharpSSH by Tamir Gal. The SharpSSH .Net libraries were invaluable for creating SSHSync.


from http://www.tamirgal.com/home/dev.aspx?Item=SharpSsh

### About SharpSSH ###

SharpSSH is a pure .NET implementation of the SSH2 client protocol suite. It provides an API for communication with SSH servers and can be integrated into any .NET application.

The library is a C# port of the JSch project from JCraft Inc. and is released under BSD style license.

SharpSSH allows you to read/write data and transfer files over SSH channels using an API similar to JSch's API. In addition, it provides some additional wrapper classes which offer even simpler abstraction for SSH communication.

SharpSSH project page at source forge: http://sourceforge.net/projects/sharpssh