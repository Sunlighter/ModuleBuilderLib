<!-- -*- coding: utf-8; fill-column: 118 -*- -->

# ModuleBuilderLib

A helper for Reflection Emit. Reflection Emit already provides ``ModuleBuilder`` and classes like that; this library
provides complementary classes like ``ModuleToBuild``, allowing the declarative specification of what you want built.

This code has been separated out of the old code for Sunlit World Scheme. I thought it would be useful independently.
Sunlit World Scheme used to be licensed under the GPL2, but since I own the code, I am relicensing the parts that
appear here under the Apache 2.0 License.

This branch brings over all the expression classes from &ldquo;Pascalesque,&rdquo; which is misnamed. Pascalesque is a
strongly-typed Scheme-like language which can be compiled to IL. It is possible to define constructors and methods
with Pascalesque bodies, or it is possible to write constructor and method bodies with IL directly.

There is no lexer or parser for this library (but I have lexer generator and parser generator libraries available as
NuGet packages which can probably be used for that purpose). For the time being it is necessary to build expression
objects with `new`.

Please be warned that the API is not stable yet. I am considering making breaking changes.
