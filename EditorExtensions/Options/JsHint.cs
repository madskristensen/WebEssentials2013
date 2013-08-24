using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;

namespace MadsKristensen.EditorExtensions
{
    class JsHintOptions : DialogPage
    {
        public JsHintOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.JsHint_maxerr, JsHint_maxerr);
            Settings.SetValue(WESettings.Keys.RunJsHintOnBuild, RunJsHintOnBuild);
            Settings.SetValue(WESettings.Keys.EnableJsHint, EnableJsHint);
            Settings.SetValue(WESettings.Keys.JsHint_ignoreFiles, IgnoreFiles);
            Settings.SetValue(WESettings.Keys.JsHintErrorLocation, (int)ErrorLocation);
            
            Settings.SetValue(WESettings.Keys.JsHint_bitwise, JsHint_bitwise);
            Settings.SetValue(WESettings.Keys.JsHint_camelcase, JsHint_camelcase);
            Settings.SetValue(WESettings.Keys.JsHint_curly, JsHint_curly);
            Settings.SetValue(WESettings.Keys.JsHint_eqeqeq, JsHint_eqeqeq);
            Settings.SetValue(WESettings.Keys.JsHint_forin, JsHint_forin);
            Settings.SetValue(WESettings.Keys.JsHint_immed, JsHint_immed);
            Settings.SetValue(WESettings.Keys.JsHint_indent, JsHint_indent);
            Settings.SetValue(WESettings.Keys.JsHint_latedef, JsHint_latedef);
            Settings.SetValue(WESettings.Keys.JsHint_newcap, JsHint_newcap);
            Settings.SetValue(WESettings.Keys.JsHint_noarg, JsHint_noarg);
            Settings.SetValue(WESettings.Keys.JsHint_noempty, JsHint_noempty);
            Settings.SetValue(WESettings.Keys.JsHint_nonew, JsHint_nonew);
            Settings.SetValue(WESettings.Keys.JsHint_plusplus, JsHint_plusplus);
            Settings.SetValue(WESettings.Keys.JsHint_quotmark, JsHint_quotmark);
            Settings.SetValue(WESettings.Keys.JsHint_regexp, JsHint_regexp);
            Settings.SetValue(WESettings.Keys.JsHint_undef, JsHint_undef);
            Settings.SetValue(WESettings.Keys.JsHint_unused, JsHint_unused);
            Settings.SetValue(WESettings.Keys.JsHint_strict, JsHint_strict);
            Settings.SetValue(WESettings.Keys.JsHint_trailing, JsHint_trailing);
            
            Settings.SetValue(WESettings.Keys.JsHint_asi, JsHint_asi);
            Settings.SetValue(WESettings.Keys.JsHint_boss, JsHint_boss);
            Settings.SetValue(WESettings.Keys.JsHint_debug, JsHint_debug);
            Settings.SetValue(WESettings.Keys.JsHint_eqnull, JsHint_eqnull);
            Settings.SetValue(WESettings.Keys.JsHint_esnext, JsHint_esnext);
            Settings.SetValue(WESettings.Keys.JsHint_evil, JsHint_evil);
            Settings.SetValue(WESettings.Keys.JsHint_expr, JsHint_expr);
            Settings.SetValue(WESettings.Keys.JsHint_funcscope, JsHint_funcscope);
            Settings.SetValue(WESettings.Keys.JsHint_globalstrict, JsHint_globalstrict);
            Settings.SetValue(WESettings.Keys.JsHint_iterator, JsHint_iterator);
            Settings.SetValue(WESettings.Keys.JsHint_lastsemic, JsHint_lastsemic);
            Settings.SetValue(WESettings.Keys.JsHint_laxbreak, JsHint_laxbreak);
            Settings.SetValue(WESettings.Keys.JsHint_laxcomma, JsHint_laxcomma);
            Settings.SetValue(WESettings.Keys.JsHint_loopfunc, JsHint_loopfunc);
            Settings.SetValue(WESettings.Keys.JsHint_multistr, JsHint_multistr);
            Settings.SetValue(WESettings.Keys.JsHint_onecase, JsHint_onecase);
            Settings.SetValue(WESettings.Keys.JsHint_proto, JsHint_proto);
            Settings.SetValue(WESettings.Keys.JsHint_regexdash, JsHint_regexdash);
            Settings.SetValue(WESettings.Keys.JsHint_scripturl, JsHint_scripturl);
            Settings.SetValue(WESettings.Keys.JsHint_smarttabs, JsHint_smarttabs);
            Settings.SetValue(WESettings.Keys.JsHint_shadow, JsHint_shadow);
            Settings.SetValue(WESettings.Keys.JsHint_sub, JsHint_sub);
            Settings.SetValue(WESettings.Keys.JsHint_supernew, JsHint_supernew);
            Settings.SetValue(WESettings.Keys.JsHint_validthis, JsHint_validthis);
            
            Settings.SetValue(WESettings.Keys.JsHint_browser, JsHint_browser);
            Settings.SetValue(WESettings.Keys.JsHint_couch, JsHint_couch);
            Settings.SetValue(WESettings.Keys.JsHint_devel, JsHint_devel);
            Settings.SetValue(WESettings.Keys.JsHint_dojo, JsHint_dojo);
            Settings.SetValue(WESettings.Keys.JsHint_mootools, JsHint_mootools);
            Settings.SetValue(WESettings.Keys.JsHint_node, JsHint_node);
            Settings.SetValue(WESettings.Keys.JsHint_nonstandard, JsHint_nonstandard);
            Settings.SetValue(WESettings.Keys.JsHint_prototypejs, JsHint_prototypejs);
            Settings.SetValue(WESettings.Keys.JsHint_rhino, JsHint_rhino);
            Settings.SetValue(WESettings.Keys.JsHint_worker, JsHint_worker);
            Settings.SetValue(WESettings.Keys.JsHint_wsh, JsHint_wsh);

            OnChanged();
            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            EnableJsHint = WESettings.GetBoolean(WESettings.Keys.EnableJsHint);
            IgnoreFiles = WESettings.GetString(WESettings.Keys.JsHint_ignoreFiles);
            RunJsHintOnBuild = WESettings.GetBoolean(WESettings.Keys.RunJsHintOnBuild);
            ErrorLocation = (WESettings.Keys.FullErrorLocation)WESettings.GetInt(WESettings.Keys.JsHintErrorLocation);
            JsHint_maxerr = WESettings.GetInt(WESettings.Keys.JsHint_maxerr);

            JsHint_bitwise = WESettings.GetBoolean(WESettings.Keys.JsHint_bitwise);
            JsHint_camelcase = WESettings.GetBoolean(WESettings.Keys.JsHint_camelcase);
            JsHint_curly = WESettings.GetBoolean(WESettings.Keys.JsHint_curly);
            JsHint_eqeqeq = WESettings.GetBoolean(WESettings.Keys.JsHint_eqeqeq);
            JsHint_forin = WESettings.GetBoolean(WESettings.Keys.JsHint_forin);
            JsHint_immed = WESettings.GetBoolean(WESettings.Keys.JsHint_immed);
            JsHint_indent = WESettings.GetInt(WESettings.Keys.JsHint_indent);
            JsHint_latedef = WESettings.GetBoolean(WESettings.Keys.JsHint_latedef);
            JsHint_newcap = WESettings.GetBoolean(WESettings.Keys.JsHint_newcap);
            JsHint_noarg = WESettings.GetBoolean(WESettings.Keys.JsHint_noarg);
            JsHint_noempty = WESettings.GetBoolean(WESettings.Keys.JsHint_noempty);
            JsHint_nonew = WESettings.GetBoolean(WESettings.Keys.JsHint_nonew);
            JsHint_plusplus = WESettings.GetBoolean(WESettings.Keys.JsHint_plusplus);
            JsHint_quotmark = WESettings.GetBoolean(WESettings.Keys.JsHint_quotmark);
            JsHint_regexp = WESettings.GetBoolean(WESettings.Keys.JsHint_regexp);
            JsHint_undef = WESettings.GetBoolean(WESettings.Keys.JsHint_undef);
            JsHint_unused = WESettings.GetBoolean(WESettings.Keys.JsHint_unused);
            JsHint_strict = WESettings.GetBoolean(WESettings.Keys.JsHint_strict);
            JsHint_trailing = WESettings.GetBoolean(WESettings.Keys.JsHint_trailing);

            JsHint_asi = WESettings.GetBoolean(WESettings.Keys.JsHint_asi);
            JsHint_boss = WESettings.GetBoolean(WESettings.Keys.JsHint_boss);
            JsHint_debug = WESettings.GetBoolean(WESettings.Keys.JsHint_debug);
            JsHint_eqnull = WESettings.GetBoolean(WESettings.Keys.JsHint_eqnull);
            JsHint_esnext = WESettings.GetBoolean(WESettings.Keys.JsHint_esnext);
            JsHint_evil = WESettings.GetBoolean(WESettings.Keys.JsHint_evil);
            JsHint_expr = WESettings.GetBoolean(WESettings.Keys.JsHint_expr);
            JsHint_funcscope = WESettings.GetBoolean(WESettings.Keys.JsHint_funcscope);
            JsHint_globalstrict = WESettings.GetBoolean(WESettings.Keys.JsHint_globalstrict);
            JsHint_iterator = WESettings.GetBoolean(WESettings.Keys.JsHint_iterator);
            JsHint_lastsemic = WESettings.GetBoolean(WESettings.Keys.JsHint_lastsemic);
            JsHint_laxbreak = WESettings.GetBoolean(WESettings.Keys.JsHint_laxbreak);
            JsHint_laxcomma = WESettings.GetBoolean(WESettings.Keys.JsHint_laxcomma);
            JsHint_loopfunc = WESettings.GetBoolean(WESettings.Keys.JsHint_loopfunc);
            JsHint_multistr = WESettings.GetBoolean(WESettings.Keys.JsHint_multistr);
            JsHint_onecase = WESettings.GetBoolean(WESettings.Keys.JsHint_onecase);
            JsHint_proto = WESettings.GetBoolean(WESettings.Keys.JsHint_proto);
            JsHint_regexdash = WESettings.GetBoolean(WESettings.Keys.JsHint_regexdash);
            JsHint_scripturl = WESettings.GetBoolean(WESettings.Keys.JsHint_scripturl);
            JsHint_smarttabs = WESettings.GetBoolean(WESettings.Keys.JsHint_smarttabs);
            JsHint_shadow = WESettings.GetBoolean(WESettings.Keys.JsHint_shadow);
            JsHint_sub = WESettings.GetBoolean(WESettings.Keys.JsHint_sub);
            JsHint_supernew = WESettings.GetBoolean(WESettings.Keys.JsHint_supernew);
            JsHint_validthis = WESettings.GetBoolean(WESettings.Keys.JsHint_validthis);

            JsHint_browser = WESettings.GetBoolean(WESettings.Keys.JsHint_browser);
            JsHint_couch = WESettings.GetBoolean(WESettings.Keys.JsHint_couch);
            JsHint_devel = WESettings.GetBoolean(WESettings.Keys.JsHint_devel);
            JsHint_dojo = WESettings.GetBoolean(WESettings.Keys.JsHint_dojo);
            JsHint_jquery = WESettings.GetBoolean(WESettings.Keys.JsHint_jquery);
            JsHint_mootools = WESettings.GetBoolean(WESettings.Keys.JsHint_mootools);
            JsHint_node = WESettings.GetBoolean(WESettings.Keys.JsHint_node);
            JsHint_nonstandard = WESettings.GetBoolean(WESettings.Keys.JsHint_nonstandard);
            JsHint_prototypejs = WESettings.GetBoolean(WESettings.Keys.JsHint_prototypejs);
            JsHint_rhino = WESettings.GetBoolean(WESettings.Keys.JsHint_rhino);
            JsHint_worker = WESettings.GetBoolean(WESettings.Keys.JsHint_worker);
            JsHint_wsh = WESettings.GetBoolean(WESettings.Keys.JsHint_wsh);
        }

        public static event EventHandler<EventArgs> Changed;

        protected void OnChanged()
        {
            if (Changed != null)
            {
                Changed(this, EventArgs.Empty);
            }
        }

        [LocDisplayName("Maximum number of errors")]
        [Description("This option suppresses warnings about mixed tabs and spaces when the latter are used for alignmnent only.")]
        [Category("Common")]
        public int JsHint_maxerr { get; set; }

        [LocDisplayName("Enable JSHint")]
        [Description("Runs JSHint in any open .js file when saved.")]
        [Category("Common")]
        public bool EnableJsHint { get; set; }

        [LocDisplayName("Ignore files")]
        [Description("A semicolon separated list of file name regex's to ignore")]
        [Category("Common")]
        public string IgnoreFiles { get; set; }

        [LocDisplayName("Run on build")]
        [Description("Runs JSHint on all .js files in the active project on build")]
        [Category("Common")]
        public bool RunJsHintOnBuild { get; set; }

        [LocDisplayName("Error location")]
        [Description("Determins where to output the JSHint errors")]
        [Category("Common")]
        public WESettings.Keys.FullErrorLocation ErrorLocation { get; set; }

        // Enforcing Options

        [LocDisplayName("Disallow bitwise operators")]
        [Description("[bitwise] This option prohibits the use of bitwise operators such as ^ (XOR), | (OR) and others. Bitwise operators are very rare in JavaScript programs and very often & is simply a mistyped &&&&.")]
        [Category("Enforcing Options")]
        public bool JsHint_bitwise { get; set; }

        [LocDisplayName("Require camelcasing")]
        [Description("[camelcase] This option allows you to force all variable names to use either camelCase style or UPPER_CASE with underscores.")]
        [Category("Enforcing Options")]
        public bool JsHint_camelcase { get; set; }

        [LocDisplayName("Enforce curly braces")]
        [Description("[curly] This option requires you to always put curly braces around blocks in loops and conditionals. JavaScript allows you to omit curly braces when the block consists of only one statement.")]
        [Category("Enforcing Options")]
        public bool JsHint_curly { get; set; }

        [LocDisplayName("Disallow == and !=")]
        [Description("[eqeqeq] This options prohibits the use of == and != in favor of === and !==.")]
        [Category("Enforcing Options")]
        public bool JsHint_eqeqeq { get; set; }

        [LocDisplayName("Filter for-in loops")]
        [Description("[forin] This option requires all for in loops to filter object's items.")]
        [Category("Enforcing Options")]
        public bool JsHint_forin { get; set; }

        [LocDisplayName("Enforce invocation parentheses")]
        [Description("[immed] This option prohibits the use of immediate function invocations without wrapping them in parentheses.")]
        [Category("Enforcing Options")]
        public bool JsHint_immed { get; set; }

        [LocDisplayName("Enforce indent size (-1 to disable)")]
        [Description("[indent] This option enforces specific tab width for your code.")]
        [Category("Enforcing Options")]
        public int JsHint_indent { get; set; }

        [LocDisplayName("Declare param before use")]
        [Description("[latedef] This option prohibits the use of a variable before it was defined.")]
        [Category("Enforcing Options")]
        public bool JsHint_latedef { get; set; }

        [LocDisplayName("Capitalize constructor functions")]
        [Description("[newcap] This option requires you to capitalize names of constructor functions.")]
        [Category("Enforcing Options")]
        public bool JsHint_newcap { get; set; }

        [LocDisplayName("Disallow arguments.caller/callee")]
        [Description("[noarg] This option prohibits the use of arguments.caller and arguments.callee.")]
        [Category("Enforcing Options")]
        public bool JsHint_noarg { get; set; }

        [LocDisplayName("No empty code blocks")]
        [Description("[noempty] This option warns when you have an empty block in your code.")]
        [Category("Enforcing Options")]
        public bool JsHint_noempty { get; set; }

        [LocDisplayName("Disallow constructor functions")]
        [Description("[nonew] This option prohibits the use of constructor functions for side-effects.")]
        [Category("Enforcing Options")]
        public bool JsHint_nonew { get; set; }

        [LocDisplayName("Disallow ++ and -- operators")]
        [Description("[plusplus] This option prohibits the use of unary increment and decrement operators.")]
        [Category("Enforcing Options")]
        public bool JsHint_plusplus { get; set; }

        [LocDisplayName("Use consistant quotation")]
        [Description("[quotmark] This option enforces the consistency of quotation marks used throughout your code.")]
        [Category("Enforcing Options")]
        public bool JsHint_quotmark { get; set; }

        [LocDisplayName("Disallow . in regex")]
        [Description("[regexp] This option prohibits the use of unsafe . in regular expressions.")]
        [Category("Enforcing Options")]
        public bool JsHint_regexp { get; set; }

        [LocDisplayName("Disallow use of undeclared var")]
        [Description("[undef] This option prohibits the use of explicitly undeclared variables. This option is very useful for spotting leaking and mistyped variables.")]
        [Category("Enforcing Options")]
        public bool JsHint_undef { get; set; }

        [LocDisplayName("Disallow unused variables")]
        [Description("[unused] This option warns when you define and never use your variables. It is very useful for general code cleanup, especially when used in addition to undef.")]
        [Category("Enforcing Options")]
        public bool JsHint_unused { get; set; }

        [LocDisplayName("Require 'strict mode'")]
        [Description("[strict] This option requires all functions to run in EcmaScript 5's strict mode.")]
        [Category("Enforcing Options")]
        public bool JsHint_strict { get; set; }

        [LocDisplayName("Disallow trailing whitespace")]
        [Description("[trailing] This option makes it an error to leave a trailing whitespace in your code. Trailing whitespaces can be source of nasty bugs with multi-line strings.")]
        [Category("Enforcing Options")]
        public bool JsHint_trailing { get; set; }

        // Relaxing Options

        [LocDisplayName("Allow missing semicolons")]
        [Description("[asi] This option suppresses warnings about missing semicolons.")]
        [Category("Relaxing Options")]
        public bool JsHint_asi { get; set; }

        [LocDisplayName("Allow assignments")]
        [Description("[boss] This option suppresses warnings about the use of assignments in cases where comparisons are expected.")]
        [Category("Relaxing Options")]
        public bool JsHint_boss { get; set; }

        [LocDisplayName("Allow debugger statements")]
        [Description("[debug] This option suppresses warnings about the debugger statements in your code.")]
        [Category("Relaxing Options")]
        public bool JsHint_debug { get; set; }

        [LocDisplayName("Allow == null")]
        [Description("[eqnull] This option suppresses warnings about == null comparisons.")]
        [Category("Relaxing Options")]
        public bool JsHint_eqnull { get; set; }

        [LocDisplayName("Allow ES.next features")]
        [Description("[esnext] This option tells JSHint that your code uses ES.next specific features such as const.")]
        [Category("Relaxing Options")]
        public bool JsHint_esnext { get; set; }

        [LocDisplayName("Allow 'eval'")]
        [Description("[evil] This option suppresses warnings about the use of eval.")]
        [Category("Relaxing Options")]
        public bool JsHint_evil { get; set; }

        [LocDisplayName("Allow expressions")]
        [Description("[expr] This option suppresses warnings about the use of expressions where normally you would expect to see assignments or function calls.")]
        [Category("Relaxing Options")]
        public bool JsHint_expr { get; set; }

        [LocDisplayName("Allow variable scoping mismatch")]
        [Description("[funcscope] This option suppresses warnings about declaring variables inside of control structures while accessing them later from the outside.")]
        [Category("Relaxing Options")]
        public bool JsHint_funcscope { get; set; }

        [LocDisplayName("Allow global strict mode")]
        [Description("[globalstrict] This option suppresses warnings about the use of global strict mode.")]
        [Category("Relaxing Options")]
        public bool JsHint_globalstrict { get; set; }

        [LocDisplayName("Allow '__iterator__'")]
        [Description("[iterator] This option suppresses warnings about the __iterator__ property.")]
        [Category("Relaxing Options")]
        public bool JsHint_iterator { get; set; }

        [LocDisplayName("Allow omitting last semicolon")]
        [Description("[lastsemic] This option suppresses warnings about missing semicolons, but only when the semicolon is omitted for the last statement in a one-line block.")]
        [Category("Relaxing Options")]
        public bool JsHint_lastsemic { get; set; }

        [LocDisplayName("Allow unsafe line breaks")]
        [Description("[laxbreak] This option suppresses most of the warnings about possibly unsafe line breakings in your code. It doesn't suppress warnings about comma-first coding style.")]
        [Category("Relaxing Options")]
        public bool JsHint_laxbreak { get; set; }

        [LocDisplayName("Allow comma first")]
        [Description("[laxcomma] This option suppresses warnings about comma-first coding style.")]
        [Category("Relaxing Options")]
        public bool JsHint_laxcomma { get; set; }

        [LocDisplayName("Allow functions inside loops")]
        [Description("[loopfunc] This option suppresses warnings about functions inside of loops.")]
        [Category("Relaxing Options")]
        public bool JsHint_loopfunc { get; set; }

        [LocDisplayName("Allow multiline strings")]
        [Description("[multistr] This option suppresses warnings about multi-line strings. Multi-line strings can be dangerous in JavaScript because all hell breaks loose if you accidentally put a whitespace in between the escape character (\\) and a new line.")]
        [Category("Relaxing Options")]
        public bool JsHint_multistr { get; set; }

        [LocDisplayName("Allow on-case switches")]
        [Description("[onecase] This option suppresses warnings about switches with just one case.")]
        [Category("Relaxing Options")]
        public bool JsHint_onecase { get; set; }

        [LocDisplayName("Allow '__proto__'")]
        [Description("[proto] This option suppresses warnings about the __proto__ property. This property is deprecated and not supported by all browsers.")]
        [Category("Relaxing Options")]
        public bool JsHint_proto { get; set; }

        [LocDisplayName("Allow unescaped '-' in regex")]
        [Description("[regexdash] This option suppresses warnings about unescaped - in the end of regular expressions.")]
        [Category("Relaxing Options")]
        public bool JsHint_regexdash { get; set; }

        [LocDisplayName("Allow 'javascript:' URLs")]
        [Description("[scripturl] This option suppresses warnings about the use of script-targeted URLs—such as javascript:")]
        [Category("Relaxing Options")]
        public bool JsHint_scripturl { get; set; }

        [LocDisplayName("Allow mixed tabs/spaces")]
        [Description("[smarttabs] This option suppresses warnings about mixed tabs and spaces when the latter are used for alignmnent only.")]
        [Category("Relaxing Options")]
        public bool JsHint_smarttabs { get; set; }

        [LocDisplayName("Allow variable shadowing")]
        [Description("[shadow] This option suppresses warnings about variable shadowing i.e. declaring a variable that had been already declared somewhere in the outer scope.")]
        [Category("Relaxing Options")]
        public bool JsHint_shadow { get; set; }

        [LocDisplayName("Allow object['member']")]
        [Description("[sub] This option suppresses warnings about using [] notation when it can be expressed in dot notation: person['name'] vs. person.name.")]
        [Category("Relaxing Options")]
        public bool JsHint_sub { get; set; }

        [LocDisplayName("Allow weird constructions")]
        [Description("[supernew] This option suppresses warnings about 'weird' constructions like new function () { ... } and new Object;.")]
        [Category("Relaxing Options")]
        public bool JsHint_supernew { get; set; }

        [LocDisplayName("Allow strict mode violations")]
        [Description("[validthis] This option suppresses warnings about possible strict violations when the code is running in strict mode and you use this in a non-constructor function.")]
        [Category("Relaxing Options")]
        public bool JsHint_validthis { get; set; }

        // Environment

        [LocDisplayName("Assume browser")]
        [Description("[browser] This option defines globals exposed by modern browsers: all the way from good ol' document and navigator to the HTML5 FileReader and other new developments in the browser world.")]
        [Category("Environment")]
        public bool JsHint_browser { get; set; }

        [LocDisplayName("Assume CouchDB")]
        [Description("[couch] This option defines globals exposed by CouchDB. CouchDB is a document-oriented database that can be queried and indexed in a MapReduce fashion using JavaScript.")]
        [Category("Environment")]
        public bool JsHint_couch { get; set; }

        [LocDisplayName("Allow console, alert etc.")]
        [Description("[devel] This option defines globals that are usually used for logging poor-man's debugging: console, alert, etc.")]
        [Category("Environment")]
        public bool JsHint_devel { get; set; }

        [LocDisplayName("Assume Dojo")]
        [Description("[dojo] This option defines globals exposed by the Dojo Toolkit.")]
        [Category("Environment")]
        public bool JsHint_dojo { get; set; }

        [LocDisplayName("Assume jQuery")]
        [Description("[jquery] This option defines globals exposed by the jQuery JavaScript library.")]
        [Category("Environment")]
        public bool JsHint_jquery { get; set; }

        [LocDisplayName("Assume MooTools")]
        [Description("[mootools] This option defines globals exposed by the MooTools JavaScript framework.")]
        [Category("Environment")]
        public bool JsHint_mootools { get; set; }

        [LocDisplayName("Assume NodeJS")]
        [Description("[node] This option defines globals available when your code is running inside of the Node runtime environment.")]
        [Category("Environment")]
        public bool JsHint_node { get; set; }

        [LocDisplayName("Allow non-standards")]
        [Description("[nonstandard] This option defines non-standard but widely adopted globals such as escape and unescape.")]
        [Category("Environment")]
        public bool JsHint_nonstandard { get; set; }

        [LocDisplayName("Assume Prototype.js")]
        [Description("[prototypejs] This option defines globals exposed by the Prototype JavaScript framework.")]
        [Category("Environment")]
        public bool JsHint_prototypejs { get; set; }

        [LocDisplayName("Assume Rhino")]
        [Description("[rhino] This option defines globals available when your code is running inside of the Rhino runtime environment.")]
        [Category("Environment")]
        public bool JsHint_rhino { get; set; }

        [LocDisplayName("Allow Web Workers")]
        [Description("[worker] This option defines globals available when your code is running inside of a Web Worker.")]
        [Category("Environment")]
        public bool JsHint_worker { get; set; }

        [LocDisplayName("Assume Windows Script Host")]
        [Description("[wsh] This option defines globals available when your code is running as a script for the Windows Script Host.")]
        [Category("Environment")]
        public bool JsHint_wsh { get; set; }
    }
}
