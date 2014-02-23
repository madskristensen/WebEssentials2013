42;
// Note that a single token to sweet.js includes matched 
// delimiters not just numbers and identifiers. For example,
// an array with all of its elements counts as one token:
[
    1,
    2,
    3
];
// One of the really important things sweet.js does is protect
// macros from unintentionally binding or capturing variables they
// weren't supposed to. This is called hygiene and to enforce hygiene 
// sweet.js must carefully rename all variable names.
var x$295;
var foo$321 = 100;
var bar$322 = 200;
var tmp$323 = 'my other temporary variable';
var tmp$325 = bar$322;
bar$322 = foo$321;
foo$321 = tmp$325;