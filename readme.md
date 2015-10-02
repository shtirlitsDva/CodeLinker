#Code Cloner ##Clones Source code between CSPROJ files**Wait, no I don't.** Code Cloner creates links in the destination `.csproj` project file to the code files in the source `.csproj`, automating the process of adding existing files as-a-link into the destination project. The files are added as relative paths if that's how they start out. If they were originally absolute paths then that is preserved.  If they already have a link I'll try not to break it. ##Why Even Is This?I ([CADbloke](https://CADbloke.com/)) write AutoCAD plugins, amongst other things. They also need to run in IntelliCAD, NanoCAD, BricsCAD, Graebert ARES, WalmartCAD or whatever else pops up, as well as a lot of versions of AutoCAD dating back to 2007 as well as AutoCAD OEM and RealDwg. Don't forget Teigha. The build settings, .NET framework version, x86/64 and project references are quite different between the platforms but they can (generally) share most of source code. *Write once, build many*.   I tried other approaches but they were [horribly](http://www.theswamp.org/index.php?topic=41868.msg497509#msg497509 "HORRIBLY") [complicated](http://www.theswamp.org/index.php?topic=49039.msg541752#msg541752 "COMPLICATED") and Nuget broke most of them. Ok, I broke them with Nuget. An online colleague came up with a [better solution](https://sites.google.com/site/aroundcad/home/autocad-net-templates) but it still wasn't simple enough for me. This Solution (see what I did there?) means you can separate the build and code concerns. I also like being able to step through the debugger and see what the code is doing in a specific build. I also like to see what `#if~ compiler directives do to my code. This ill never be simple but I wanted something that wasn't a rocket science in itself.  Here are the requirements straight from my Trello board before I wrote this ...  ###Code Cloner Requirements - One project that manages the common Source Code. This is necessary because files are included in different ways, eg. `XAML.CS` files depend on their `XAML` file etc. This needs to be managed in one place - **one version of the truth** and it needs to be managed by a `csproj` file.  - The canonical project actually builds and is the primary development project, usually the current version of AutoCAD. It is where you will pend most of your time. The destination projects are for build configs and tweakage to make it work for the target platform - Adding a build config and debugging should be as easy as adding another project to the build collection with minimal preparation. It should know all about the common Source Code. It could have its own code too that overrides the canonical source with specific code for this build - The primary development project knows nothing of any clones of it and is not affected by them. ever. - Source Code should be displayed in the build project. All `#If` compiler directives should work. Debugging should step through the code - Source code should be synced between projects without user intervention, automatically so it is not overlooked. - Easy to maintain. If you accidentally add the code to the wrong place, just move it to the right place and it all heals itself. - Unloading Build projects should not affect anything - they should still update and all that. Nothing should break. - Keep all the Build projects in a separate folder out of the way to avoid clutter. - Build projects should also be able to have code that compiles as well as the canonical code. This is for `Overrides` etc.  - it should easily bolt on top of something that already exists with minimal carnage.###a Workflow I just made up - create your original project - decide you want to build a version of it - create a solution folder in the root called `_Builds`. I do this so it's at the top of the *Solution Explorer* and easy to find. Also, it's out of the way of the main project. - create the destination Project, it's the same type of project as the one you want to clone - Add the clone zone" placeholders (I'll tell you about it later) in the destination project - drop CodeCloner.exe in the Solution root. It's not massive so don't panic. - run it (patience Grasshopper, you'll find out soon) - add references and fix things - carry on with your original project - rinse & repeat - decide on a strategy for keeping the destination Projects up-to-date by...###a couple of suggested ways to run this    - Visual Studio Project pre-build process, add the command line(s) with args to your source project's `Build Events` tab in the `Project Properties` window, in the `Pre-build event command line:` text box. If you add it to the source project it updates its destinations whenever you build it. The targets don't have to be loaded in Visual Studio, actually life is (much) easier if they aren't because VS will want to reload them if/when they change.      -  Set up a `.cmd` or `.bat` with a command line or few and run me when you think it all needs a refresh. -  Diff all the things after a clone to make sure I haven't eaten a kitten by mistake.There is a log file called *CodeClonerLog.txt* saved in the same folder as the executable. If you use me as a *pre-build process* The Visual Studio output window will have some summary information but the details will be in the log file. Good luck finding anything in the output window anyway ###The Command Line...`CODECLONER /?`  `CODECLONER [Folder] [/s]`  `CODECLONER [Source.csproj] [Destination.csproj]`    `/?` &nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;    Help!  `Folder` &nbsp;&nbsp;&nbsp; Clones the source(s) into all `CSPROJ` files in the folder  `/s` &nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;    Also iterates all subfolders. You just forgot to add this, right?`Source.csproj`      &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;  Path to the CSPROJ with the source to be cloned.  `Destination.csproj` &nbsp;&nbsp;&nbsp;  Path to ... duh.- Wrap paths with spaces in double quotes. - Paths can (probably should!) be relative,.  - Relative paths on the command line are relative to the executable- Relative paths in the destination `.csproj` are relative the that destination `.csproj`  - `Source.csproj` is optional on the command line , if you specify one  `.csproj` file it is the destination, in which case you need to have the source project in the placeholder. If you specify a source this overrides any sources you have set in the destination `.csproj` file's placeholder###The Destination CSPROJ file needs this XML comment placeholder...    <!-- CodeCloner    Source: PathTo\\NameOfProject.csproj <== this is optional    Exclude: PathTo\\FileToBeExcluded.cs <== this is optional    -->        <!-- EndCodeCloner -->This is where the code goes.##iFAQa**i**n**F**requently **A**sked **Q**uestion **a**nswers  - You can add more code or whatever to the destination project, just don't put it between the "clone zone" placeholders or it will be removed in the next clone.  - You will probably have to build your solution twice if a project is changed by a code-cloning process, mostly because of parallel builds and all that, the project was probably building while it was also being cloned. Expect that to be troublesome at times.- You may specify multiple `Source:` projects. I don't know wildcards so you will have to list them all individually.  - I don't check for duplicated things, especially from multiple source projects.  - If you don't specify a source in the placeholder it better be in the command line call.  - `Exclude:` file or path. No wildcards here either. It is a simple `String.Contains()` filter. The log will tell you what was excluded and why.- If you specify multiple `Exclude:` items then they must be on separate lines. - Every Code Clone will re-clone the source CSPROJ into the "clone zone" between the XML comment placeholders.  - ALL code inside this "clone zone" is refreshed every time. OK?- I usually drop the executable in the solution root folder (it makes working out relative paths easier) and check it in to Git but ignore the *CodeClonerLog.txt* log file.- The log file is continuous - delete it to reset it or you can mess with it in a text editor. - It doesn't replicate Project references, or any references at all.- Shared Projects don't have the source code right in front of you, don't do 3rd party libraries etc.. ok, if they do then I got it wrong. Tell me how.- This is a v1 - use it at your own risk. I may eat your kittens. I don't think it will but I've been wrong before. See the Refund Policy below for more details.- does it work with VB? Nup. Feel free to change that but I don't VB so I didn't try.- refactoring code with `#if` compiler directives can be tricky- if you edit a source code file in any project it is cloned to, you edit it *everywhere* - they are all linked to the one file in the original source.- I haven't tried this but I bet someone will - I assume that cloning a clone with deliver you a link to the original source. If it doesn't work out like that - WHAT WERE YOU THINKING?If you weren't good at `XML` & `csproj` files before then you will probably learn a reasonable amount about it using this - expect to occasionally break things. If you're not willing to occasionally cut your fingers on an angle-bracket then this is not for you. Git will get you out of most trouble but knowing a little about the internals of `.csproj` (related to MSBuild) will help. It's `XML`, you won't bluescreen anything if you edit it and get it wrong. Just make sure you commit often.  Most things need to be inside a `<PropertyGroup>` or [`<ItemGroup>`](https://msdn.microsoft.com/en-us/library/646dk05y.aspx). `<ItemGroup>` is the one we're interested in messing with. Visual Studio has a way of changing things you've edited, especially when it comes to wildcards - I like to add a copy of my edits as an XML comment to preserve the original although the commit history is just as good for this so I'll stop doing that.  If you are using Notepad++ then try this [Syntax Highlighter](https://gist.github.com/CADbloke/7478607).So far there has been no magic bullet for write once, build on many tenuously-related platforms. I have tried a plethora of build configs inside one project (my record is 72) but that breaks catastrophically if you add a Nuget reference or something like that. I think the Solution (sorry, I should stop that) lies in separate projects with build specifics hauling their core source code in from a one-version-of-the-truth. You can't just import a whole Project because that drags in all the build settings, references etc. At least with this method the fragility is in the destination `csproj`, not in the original source. I can only automate so much, you will still have to get your hands a little dirty.    Diffing the source and destination projects should give you an idea on how it went and what actually happened. ####This is the bare-bones placeholder with no options specified...    <!-- CodeCloner -->        <!-- EndCodeCloner -->####This is a random *pre-build process*line example I just made up...    codecloner \SourceProject.csproj \Destination1\Destination1.csproj    codecloner \SourceProject.csproj \Destination2\Destination2.csproj    codecloner \Destination3\Destination3.csproj`Destination3.csproj` in this example will need to have a source `.csproj` specified in the XML comment placeholder or it will crash out. The log will let you know. Maybe.  command line usage looks the same, only in a `.bat` file and paths are [relative to the `.bat` file](http://stackoverflow.com/questions/14936625/relative-path-in-bat-script)More Info & Source at [https://github.com/CADbloke/CodeCloner](https://github.com/CADbloke/CodeCloner "Code Cloner @ GitHub"). I'm also happy to hear of any issues &/or suggestions (Pull Requests are even better!).   **Debugging?** In `Program.cs` look at the top for      static void Main(string[] args)    {       // System.Diagnostics.Debugger.Launch(); // to find teh bugs####Refund policyGet your credit card pic retweeted at [https://twitter.com/needadebitcard](https://twitter.com/needadebitcard "DO NOT DO THIS !!") and I will refund you twice the price you paid for this. For further details see the [Extended Refund Policy](http://www.seobook.com/freetards "This is not you, right?")   ####Code Cloner by CADblokeoh, you're still here.  ok  um  Don't be afraid to edit the XML `.csproj` files. This works ...      <Compile Include="$(Codez)\z.Libraries\diff-match-patch\DiffMatchPatch\*.cs">    <Link>Libs\%(RecursiveDir)%(Filename)%(Extension)</Link>    </Compile>...and will give you all the C# files from the source folder as linked files in your destination project. `$(Codez)` is a Windows Environment Variable I use on my PCs. I also could have used `\*.*` at the end. This is one of those things Visual Studio might break on you, adding a file in the folder full of wildcard-linked files may break them out to separate entries. Or not. Depends on the wind.