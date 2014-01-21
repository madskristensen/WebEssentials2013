using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using MadsKristensen.EditorExtensions.Compilers;
using Microsoft.CSS.Core;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    [TestClass]
    public class LessDocumentTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext c) { SettingsStore.EnterTestMode(); }

        [TestMethod]
        public async Task SelectorExpansionTest()
        {
            #region LESS sources
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
@"  // Taken from http://blog.slaks.net/2013-09-29/less-css-secrets-of-the-ampersand/
a {
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
    blockquote& {
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
@"  // Taken from https://github.com/less/less.js/blob/master/test/less/selectors.less
h1, h2, h3 {
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
    .qux& {
      display: inline-block;
    }
    .qux & .biz {
      display: none;
    }
  }
}

.b {
 &.c {
  .a& {
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
  .foo&.bar {
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
            @"// Taken from https://github.com/less/less.js/blob/master/test/less/rulesets.less
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

            foreach (var lessCode in testSources)
            {
                var cssCode = await new LessCompiler().CompileSourceAsync(lessCode, ".less");
                var lessDoc = new LessParser().Parse(lessCode, false);
                var cssDoc = new CssParser().Parse(cssCode, false);

                var cssSelectors = new CssItemAggregator<string>(false) { (Selector s) => CssExtensions.SelectorText(s) }.Crawl(cssDoc);

                var lessSelectors = new CssItemAggregator<RuleSet>(true) { (RuleSet rs) => rs }.Crawl(lessDoc)
                                                .Where(rs => rs.Block.Declarations.Any())   // Skip selectors that don't have any rules; these won't end up in the CSS
                                                .SelectMany(rs => LessDocument.GetSelectorNames(rs, LessMixinAction.Literal))
                                                .ToList();

                lessSelectors.Should().Equal(cssSelectors);
            }
        }

        [TestMethod]
        public void TestMixinExpansion()
        {
            var lessCode = @"a {
                                .myMixin(@p) {
                                    b, code {
                                    }
                                }
                            }";

            var lessDoc = new LessParser().Parse(lessCode, false);
            var lessBlocks = new CssItemAggregator<RuleSet>(true) { (RuleSet rs) => rs }.Crawl(lessDoc).ToList();
            // Remove all but the deepest blocks
            while (0 < lessBlocks.RemoveAll(c => lessBlocks.Any(c.IsParentOf)))
                ;

            var literalExpansions = lessBlocks.SelectMany(rs => LessDocument.GetSelectorNames(rs, LessMixinAction.Literal)).ToList();
            literalExpansions.Should().Equal(new[] { "a .myMixin(@p) b", "a .myMixin(@p) code" });

            var skipExpansions = lessBlocks.SelectMany(rs => LessDocument.GetSelectorNames(rs, LessMixinAction.Skip)).ToList();
            skipExpansions.Should().BeEmpty();

            var nestedExpansions = lessBlocks.SelectMany(rs => LessDocument.GetSelectorNames(rs, LessMixinAction.NestedOnly)).ToList();
            nestedExpansions.Should().Equal(new[] { "«mixin .myMixin» b", "«mixin .myMixin» code" });
        }
    }
}
