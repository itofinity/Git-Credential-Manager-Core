# Development and debugging

## Cocoapods
The osx projects require Cocoapods to be installed

https://cocoapods.org/

$sudo gem install cocoapods

## Xcode

$ xcodebuild
xcode-select: error: tool 'xcodebuild' requires Xcode, but active developer directory '/Library/Developer/CommandLineTools' is a command line tools instance

$ xcodebuild


Agreeing to the Xcode/iOS license requires admin privileges, please run “sudo xcodebuild -license” and then retry this command.

$ sudo xcodebuild -license


You have not agreed to the Xcode license agreements. You must agree to both license agreements below in order to use Xcode.

Hit the Enter key to view the license agreements at '/Applications/Xcode.app/Contents/Resources/English.lproj/License.rtf'

Xcode and Apple SDKs Agreement

PLEASE SCROLL DOWN AND READ ALL OF ...

Software License Agreements Press 'space' for more, or 'q' to quit


# publish/install

dotnet publish --configuration=MacDebug

sudo installer -pkg /Users/mminns/projects/github.com/mminns/Git-Credential-Manager-Core/out/osx/Installer.Mac/pkg/Debug/gcmcore-osx-2.0.79.24926.pkg -target /


## testing

atlas-run-standalone --product bitbucket

install 2FA plugin

2FA for Bitbucket: U2F & TOTP -> works for web app but does not secure Git traffic



## ssh keys

create bbS compatible keys

12:06 $ openssl req -x509 -sha1 -nodes -days 365 -newkey rsa:2048 -keyout gcm-rsa-sha1-2048-private-1.key -out gcm-rsa-sha1-2048-private-1-certificate_pub.crt
12:06 $ openssl rsa -in gcm-rsa-sha1-2048-private-1.key  -pubout -out gcm-rsa-sha1-2048-private-1.pub

these can be associated with an incoming generic applink

associate the relevant key in the client

12:08 $ git config --global credential.localhost:7990.bbsOAuthConsumerSecret /Users/mminns/projects/github.com/mminns/Git-Credential-Manager-Core/keys/gcm-rsa-sha256-2048-private-1.key
12:08 $ git config --global credential.localhost.bbsOAuthConsumerSecret /Users/mminns/projects/github.com/mminns/Git-Credential-Manager-Core/keys/gcm-rsa-sha256-2048-private-1.key

