#Sample blog post
This blog post talks about how to use powerful features like **delegates** and **lambda expressions** in C# & VB.Net.
These features bring our favorite langauges much closer to the newer realms of _functional programming_.

Note that `snake_cased_identifiers` should _not_ be used in .Net.

```VB
AddHandler AppDomain.CurrentDomain.AssemblyLoad, Sub(s, e) e.LoadedAssembly.GetName()
Assembly.Load(Await Task.Run(Function() File.ReadAllBytes(".../MyFile.dlll")))
```

```C#
AppDomain.CurrentDomain.AssemblyLoad += (s, e) => e.LoadedAssembly.GetName(123).G;
Assembly.Load(await Task.Run(() => File.ReadAllBytes(".../MyFile.dlll")));
```

>> #Quote from some other guy
>> Functional programming is cool!
>> 
>> ```Javascript
>> var numbers = [1, 2, 3, 4, 5, 6, 7, 8];
>> var cubes = numbers.map(function(n) { return n * n * n;});
>> console.log(cubes);
>> ```
>> 
>> ```C#
>> var numbers = new[] { 1, 2, 3, 4, 5, 6, 7, 8  };
>> var longerCubes = from n in numbers
>>                   let cube = n * n * n
>>                   where cube.ToString().Length > n.ToString().Length
>>                   select cube;
>> Console.WriteLine(String.Join(",", longerCubes));
>> ```
