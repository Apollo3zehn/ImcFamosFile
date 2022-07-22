# ImcFamosFile

[![GitHub Actions](https://github.com/Apollo3zehn/ImcFamosFile/actions/workflows/build-and-publish.yml/badge.svg)](https://github.com/Apollo3zehn/ImcFamosFile/actions) [![NuGet](https://img.shields.io/nuget/v/ImcFamosFile.svg?label=Nuget)](https://www.nuget.org/packages/ImcFamosFile)

ImcFamosFile is a .NET Standard 2.1 library to read and write imc FAMOS files (.dat, .raw) of version 2.

This library creates groups containing texts, single values and channels of different types (e.g.: data with `equidistant time`, with `monotonous increasing time` or `characteristic curves` (XY data)).

Below is a screenshot of FAMOS after opening the [sample](sample/ImcFamosFileSample/Program.cs) file and plotting the power curve of a wind turbine:

![Sample preview.](/doc/images/sample_preview.png)

ImcFamosFile is capable of reading single channels or all data at once and provide it as byte array or optionally casted to the actual data type using the new .NET Core [Span<T>](https://docs.microsoft.com/de-de/dotnet/api/system.span-1?view=netcore-3.0) feature (which allows efficient C-like casting of pointers to different data types without any copy operations).

Please see the [introduction](https://apollo3zehn.github.io/ImcFamosFile/how_to/1_introduction.html) to get a more detailed description on how to use this library!