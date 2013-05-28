EtlGate ReadMe
===

## DESCRIPTION

EtlGate is a library to facilitate reading delimited files. 

It has the following features:

* comma delimited (CSV) data reader provided
* based on an any-sequence delimited data reader
* reads from a stream and returns IEnumerable so there is virtually no limit to your data size


## HOW TO BUILD

The build script requires Ruby with rake installed.

1. Run `InstallGems.bat` to get the ruby dependencies (only needs to be run once per computer)
1. open a command prompt to the root folder and type `rake` to execute rakefile.rb

If you do not have ruby:

1. You need to create a src\CommonAssemblyInfo.cs file. Go.bat will copy src\ 
  * go.bat will copy src\CommonAssemblyInfo.cs.default to src\CommonAssemblyInfo.cs
1. open src\EtlGate.sln with Visual Studio and Build the solution

## License		

[MIT License][mitlicense]

[mitlicense]: http://www.opensource.org/licenses/mit-license.php
