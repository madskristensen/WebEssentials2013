using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using MadsKristensen.EditorExtensions.Scss;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.Editor;

namespace WebEssentialsTests
{
    [TestClass]
    public class ScssDocumentTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext c) { SettingsStore.EnterTestMode(); }

        [TestMethod]
        public async Task SelectorExpansionTest()
        {
            #region Scss sources
            var testSources = new[]{
@"
@media all {
    a {
        @media all {
            @media all {
                b {
                    color: goldenrod;
                    em {
                        color: goldenrod;
                    }
                }
            }
        }
    }
}
",
@"a {
    color: blue;
    &:hover {
        color: green;
    }
}
form a {
    color: purple;
    body.QuietMode & {
        color: black;
    }
}
.quoted-source {
    background: #fcc;
    blockquote & {
        background: #fdc;
    }
}
.btn.btn-primary.btn-lg[disabled] {
    & + & + & {
        margin-left: 10px;
    }
}
p, blockquote, ul, li {
    border-top: 1px solid gray;
    & + & {
        border-top: 0;
    }
}
",
@"h1, h2, h3 {
  a, p {
    &:hover {
      color: red;
    }
  }
}

#all { color: blue; }
#the { color: blue; }
#same { color: blue; }

ul, li, div, q, blockquote, textarea {
  margin: 0;
}

td {
  margin: 0;
  padding: 0;
}

td, input {
  line-height: 1em;
}

a {
  color: red;

  &:hover { color: blue; }

  div & { color: green; }

  p & span { color: yellow; }
}

.foo {
  .bar, .baz {
    & .qux {
      display: block;
    }
    .qux & {
      display: inline;
    }
    .qux & {
      display: inline-block;
    }
    .qux & .biz {
      display: none;
    }
  }
}

.b {
 &.c {
  .a & {
   color: red;
  }
 }
}

.b {
 .c & {
  &.a {
   color: red;
  }
 }
}

.p {
  .foo &.bar {
    color: red;
  }
}

.p {
  .foo &.bar {
    color: red;
  }
}

.foo {
  .foo + & {
    background: amber;
  }
  & + & {
    background: amber;
  }
}

.foo, .bar {
  & + & {
    background: amber;
  }
}

.foo, .bar {
  a, b {
    & > & {
      background: amber;
    }
  }
}

.other ::fnord { color: red }
.other::fnord { color: red }
.other {
  ::bnord {color: red }
  &::bnord {color: red }
}
",
@"// Taken from https://github.com/Scss/Scss.js/blob/master/test/Scss/rulesets.Scss
#first > .one {
  > #second .two > #deux {
    width: 50%;
    #third {
      &:focus {
        color: black;
        #fifth {
          > #sixth {
            .seventh #eighth {
              + #ninth {
                color: purple;
              }
            }
          }
        }
      }
      height: 100%;
    }
    #fourth, #five, #six {
      color: #110000;
      .seven, .eight > #nine {
        border: 1px solid black;
      }
      #ten {
        color: red;
      }
    }
  }
  font-size: 2em;
}
"};
            #endregion

            var scssFactory = CssParserLocator.FindComponent(ContentTypeManager.GetContentType(ScssContentTypeDefinition.ScssContentType));
            foreach (var scssCode in testSources)
            {
                var cssCode = await new ScssCompiler().CompileSourceAsync(scssCode, ".scss");
                var scssDoc = scssFactory.CreateParser().Parse(scssCode, false);
                var cssDoc = new CssParser().Parse(cssCode, false);

                var cssSelectors = new CssItemAggregator<string>(false) { (Selector s) => CssExtensions.SelectorText(s) }.Crawl(cssDoc);

                var scssSelectors = new CssItemAggregator<RuleSet>(true) { (RuleSet rs) => rs }.Crawl(scssDoc)
                                                .Where(rs => rs.Block.Declarations.Any())   // Skip selectors that don't have any rules; these won't end up in the CSS
                                                .SelectMany(rs => ScssDocument.GetSelectorNames(rs, ScssMixinAction.Literal))
                                                .ToList();

                scssSelectors.Should().BeEquivalentTo(cssSelectors);
            }
        }

        [TestMethod]
        public void TestMixinExpansion()
        {
            var scssFactory = CssParserLocator.FindComponent(ContentTypeManager.GetContentType(ScssContentTypeDefinition.ScssContentType));
            var scssCode = @"a {
                                @mixin myMixin($p) {
                                        b, code {
                                            width: $p+10+px;
                                        }
                                    }
                                    @include myMixin(1)
                                }
                            ";

            var scssDoc = scssFactory.CreateParser().Parse(scssCode, false);
            var scssBlocks = new CssItemAggregator<RuleSet>(true) { (RuleSet rs) => rs }.Crawl(scssDoc).ToList();
            // Remove all but the deepest blocks
            while (0 < scssBlocks.RemoveAll(c => scssBlocks.Any(c.IsParentOf)))
                ;

            var literalExpansions = scssBlocks.SelectMany(rs => ScssDocument.GetSelectorNames(rs, ScssMixinAction.Literal)).ToList();
            literalExpansions.Should().Equal(new[] { "a b", "a code" });

            var skipExpansions = scssBlocks.SelectMany(rs => ScssDocument.GetSelectorNames(rs, ScssMixinAction.Skip)).ToList();
            skipExpansions.Should().BeEmpty();
        }
    }
}
