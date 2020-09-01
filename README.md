EtlGate ReadMe
===
### Description

EtlGate is a library to facilitate reading delimited files. 

It has the following features:

* comma delimited (CSV) data reader provided
* based on an any-sequence delimited data reader
* reads from a stream and returns IEnumerable so there is virtually no limit to your data size


### How To Build:

The build script requires Ruby with rake installed.

1. Run `InstallGems.bat` to get the ruby dependencies (only needs to be run once per computer)
1. open a command prompt to the root folder and type `rake` to execute rakefile.rb

If you do not have ruby:

1. open src\EtlGate.sln with Visual Studio and Build the solution

### License

[MIT License][mitlicense]

This project is part of [MVBA's Open Source Projects][MvbaLawGithub].

If you have questions or comments about this project, please contact us at <mailto:opensource@mvbalaw.com>.

[MvbaLawGithub]: http://mvbalaw.github.io/
[mitlicense]: http://www.opensource.org/licenses/mit-license.php
