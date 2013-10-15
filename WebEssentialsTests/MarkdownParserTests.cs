using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MadsKristensen.EditorExtensions.Classifications.Markdown;
using Microsoft.Web.Core;

namespace WebEssentialsTests
{
    [TestClass]
    public class MarkdownParserTests
    {
        static List<string> ParseCodeBlocks(string markdown)
        {
            var retVal = new List<string>();
            var parser = new MarkdownParser(new CharacterStream(markdown));
            parser.ArtifactFound += (s, e) => retVal.Add(markdown.Substring(e.Artifact.InnerRange.Start, e.Artifact.InnerRange.Length));
            parser.Parse();
            return retVal;
        }


        [TestMethod]
        public void TestInlineCodeBlocks()
        {
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"Hi there! `abc` Bye!"));
            CollectionAssert.AreEquivalent(new[] { "abc", "def" }, ParseCodeBlocks(@"Hi there! `abc``def` Bye!"));
            CollectionAssert.AreEquivalent(new[] { " b " }, ParseCodeBlocks(@"a` b `c"));
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"`abc`"));
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks("\n`abc`\n"));

            CollectionAssert.AreEquivalent(new[] { "" }, ParseCodeBlocks(@"a ``abc`"));
            CollectionAssert.AreEquivalent(new[] { "" }, ParseCodeBlocks(@"a ``v"));
            CollectionAssert.AreEquivalent(new string[0], ParseCodeBlocks(@"a \`v`"));
        }

        [TestMethod]
        public void TestIndentedCodeBlocks()
        {
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"Hi there!

    abc
Bye!"));
            CollectionAssert.AreEquivalent(new[] { "abc", "def" }, ParseCodeBlocks(@"Hi there!

    abc
    def
Bye!"));
//            CollectionAssert.AreEquivalent(new[] { "abc", "def" }, ParseCodeBlocks(@"Hi there!
// 1. List!
//
//        abc
// * List!
//
//        def
//
// - More
//    Not code!
//Bye!"));
            //CollectionAssert.AreEquivalent(new[] { "Code!" }, ParseCodeBlocks(" 1. abc\n\n  \t  Code!"));

           // CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks("Hi there!\n\tabc\nBye!"));
            CollectionAssert.AreEquivalent(new string[0], ParseCodeBlocks(@"Hi there!
    abc
Bye!"));
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"
    abc
"));
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"
    abc"));
        }

        [TestMethod]
        public void TestQuotedIndentedCodeBlocks()
        {
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"Hi there!

>     abc
Bye!"), "Unquoted blank line counts as block boundary");
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>
>     abc
Bye!"), "Quoted blank line counts as block boundary");
            CollectionAssert.AreEquivalent(new[] { "abc", " def", "ghi" }, ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>
>>>     abc
     def
>     ghi
Bye!"), "Quoted blank line counts as block boundary");
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!

>>>>     abc
Bye!"));
            CollectionAssert.AreEquivalent(new string[0], ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>     > abc
Bye!"), "Missing blank line");
            CollectionAssert.AreEquivalent(new[] { "> abc" }, ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>>     > abc
Bye!"), "Deeper indent counts as block boundary");
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"Hi there!
>>>>
>     abc
Bye!"), "Less-deep indent still counts");

            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"
>     abc"));
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@">     abc"));

//            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(" >\t> \tabc"));
//            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(" >\t > \tabc"));
//            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(" >  \t > \tabc"));
//            CollectionAssert.AreEquivalent(new[] { "> abc" }, ParseCodeBlocks(" >  \t  > abc"));
//            CollectionAssert.AreEquivalent(new[] { "> abc" }, ParseCodeBlocks(" >\t  > abc"));
        }

        [TestMethod]
        public void TestFencedCodeBlocks()
        {
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"Hi there!

```
abc
```
Bye!"));
            CollectionAssert.AreEquivalent(new[] { "abc", "def" }, ParseCodeBlocks(@"Hi there!

~~~
abc
def
~~~

Bye!"));
            CollectionAssert.AreEquivalent(new[] { "abc", "", "", "~~~" }, ParseCodeBlocks(@"Hi there!

```
abc


~~~
```
Bye!"));
            CollectionAssert.AreEquivalent(new[] { "    abc" }, ParseCodeBlocks(@"Hi there!
```
    abc
```
Bye!"));
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"```
abc
```"));
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"```
abc
"));
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"```
abc"));
        }

        [TestMethod]
        public void TestQuotedFencedCodeBlocks()
        {
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"Hi there!
> ```
> abc
```
Bye!"), "Unquoted fence counts as block boundary");
            CollectionAssert.AreEquivalent(new[] { "    abc" }, ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>```
>     abc
>>>>```
Bye!"), "Quoted fence counts as block boundary");

            // TODO: GitHub counts this as a quote; not sure why.
            // Apparently, they treat it as wrapping instead of a terminator.
            CollectionAssert.AreEquivalent(new[] { ">>>>abc" }, ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
```
>>>>abc
```
Bye!"));
            CollectionAssert.AreEquivalent(new[] { ">     > abc" }, ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>```
>>>>>     > abc
>>>>```
Bye!"), "Deeper indent doesn't count as block boundary");
            CollectionAssert.AreEquivalent(new[] { "    abc" }, ParseCodeBlocks(@"Hi there!
>>>>```
>     abc
```
Bye!"), "Less-deep indent still counts");

            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@">```
>abc
```"));
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@">```
>abc"));
        }
    }
}
