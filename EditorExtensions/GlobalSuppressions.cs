// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Code Analysis results, point to "Suppress Message", and click 
// "In Suppression File".
// You do not need to add suppressions to this file manually.

using System.Diagnostics.CodeAnalysis;

// ConfOxide calls otherwise-unused private setters using Reflection
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#General")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#CodeGen")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#BrowserLink")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#Sprite")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#TypeScript")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#Css")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#Html")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#JavaScript")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#Less")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#Scss")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#CoffeeScript")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#LiveScript")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#Markdown")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#SweetJs")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.WESettings.#Handlebars")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.Settings.HtmlSettings.#ImageDropFormats")]

// DispatcherPriorityAwaiter is used only for await and does not follow normal rules.  (copied from .Net source)
[assembly: SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Scope = "type", Target = "MadsKristensen.EditorExtensions.DispatcherPriorityAwaitable")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Scope = "type", Target = "MadsKristensen.EditorExtensions.DispatcherPriorityAwaiter")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "MadsKristensen.EditorExtensions.DispatcherPriorityAwaiter.#IsCompleted")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "MadsKristensen.EditorExtensions.DispatcherPriorityAwaiter.#GetResult()")]
[assembly: SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Scope = "member", Target = "MadsKristensen.EditorExtensions.DispatcherPriorityAwaitable.#GetAwaiter()")]
[assembly: SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "DispatcherPriorityAwaiter", Scope = "member", Target = "MadsKristensen.EditorExtensions.DispatcherPriorityAwaiter.#OnCompleted(System.Action)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MadsKristensen.EditorExtensions.MinifyFileMenu+MinifyFileCommand.#FileExtensionRegistry")]
