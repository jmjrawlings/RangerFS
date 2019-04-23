(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/RangerFS/net472"

(**
RangerFS - Intervals for F#
========================

RangerFS is a library for creating, manipulating, composing and comparing closed intervals in a functional manner.  
Inspiration is taken from [Allen's Interval Algebra](https://en.wikipedia.org/wiki/Allen%27s_interval_algebra) in defining interval relations.

The generic type `Range<'t>` allows intervals to be constructed for any type that satisfies the `comparison` constraint including:

- int
- float
- DateTime
- TimeSpan
- char
- user types

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
the project and submit pull requests. 

The library is available under the MIT license - for more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/fsprojects/RangerFS/tree/master/docs/content
  [gh]: https://github.com/fsprojects/RangerFS
  [issues]: https://github.com/fsprojects/RangerFS/issues
  [readme]: https://github.com/fsprojects/RangerFS/blob/master/README.md
  [license]: https://github.com/fsprojects/RangerFS/blob/master/LICENSE.txt
*)
