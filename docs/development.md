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

