(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/RangerFS/net47"

(**
RangerFS
======================

Generic Intervals for .NET

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      RangerFS library can be <a href="https://nuget.org/packages/RangerFS">installed from NuGet</a>:
      <pre>PM> Install-Package RangerFS</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Using RangerFS with Paket
------------------------

Since RangerFS is a single-file module, it can be referenced using [Paket GitHub dependencies][deps].
To do so, add following line to your `paket.dependencies` file:

    github jmjrawlings/RangerFS src/RangerFS/Range.fs

and following line to your `paket.references` file for the desired project:

    File:Range.fs . 

Samples & documentation
-----------------------

 * [Tutorial](tutorial.html) contains more detailed examples.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. This includes additional brief samples on using most of the
   functions.
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/fsprojects/RangerFS/tree/master/docs/content
  [gh]: https://github.com/fsprojects/RangerFS
  [issues]: https://github.com/fsprojects/RangerFS/issues
  [readme]: https://github.com/fsprojects/RangerFS/blob/master/README.md
  [license]: https://github.com/fsprojects/RangerFS/blob/master/LICENSE.txt
*)
