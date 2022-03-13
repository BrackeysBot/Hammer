# Contribution Guidelines

When contributing to this repository, please first discuss the change you wish to make by contacting one of the bot developers in the Discord server, or by creating a [discussion](https://github.com/oliverbooth/BrackeysBot/discussions) here in this repository.

Please note we have a code of conduct, please follow it in all your interactions with the project.

## Pull Request Process
1. Update [README.md](README.md) outlining any necessary changes made to the project - do not leave this down to the repository owners.
2. Do not increase any version numbers. This process is done by us when we feel it necessary to do so.
3. This repository, and its child repositories, follow Microsoft's [C# coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) and [.NET design guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/). Please adhere to these conventions and guidelines. Pull requests which do not fall in line with the code style will be left open until this is adhered to (and may be closed at any time if we feel the changes will not be agreed upon.)

## Code Style
Where Microsoft's conventions do not suffice, an .editorconfig is provided in the repository which should integrate with Visual Studio or Rider to automate the process. This .editorconfig should roughly coincide with [StyleCop rules](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/tree/master/documentation), with some minor exceptions.

### Comments
**Please use comments sparingly!** The use of comments to outline what a particular block of code is doing is usually indicative of the code is not being clear enough in its own right. If you feel that a comment is required to clarify logic, consider refactoring the code to includes having meaningfully named variables and methods so that a comment is redundant.

An example of comment types which would be unacceptable:
```cs
foreach (char character in someString) // loop through every char in the string
{
    Console.WriteLine(character); // print out each character on a new line
}
```

The exception to this is if the comment is explaining the "why" rather than the "what". A comment which outlines the rationale behind a specific solution is acceptable. For example:
```cs
for (var index = 0; index < someString.Length; index++) // cheaper than foreach, no allocation of CharEnumerator
{
    char character = someString[index];
    Console.WriteLine(character);
}
```
In such a case, the comment is not explaining what the code does - but why it does it that way, rather than a different way. This type of comment is accepted and encouraged.
