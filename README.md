# wk
Walks a filesystem running commands. Windows command-line utility

Build using your favorite version of .net like this:

    c:\windows\microsoft.net\framework64\v4.0.30319\csc.exe wk.cs /nologo /nowarn:0162 /nowarn:0168

Usage

    Usage: wk [/d] [/e] [/f] [/o] [/p:<path>] [/s:<filespec>] command
      Walk:      Recursively walk the filesystem executing commands on files or directories.
      arguments: command     The command invoked.
                 /d          Walk directories, not files. Executes command in each directory.
                 /e          Display errors when folders/files can't be enumerated. Probably access denied.
                 /f          Flat. Process files (or folders with /d) in [path] and don't recurse.
                 /o          One process runs at a time. Default is based on core count.
                 /p          The root path for processing. Default is .\ (the current directory).
                 /q          Quiet; only generate output from child processes. Overrides /e
                 /s          The files for which command is invoked. Default is *.* (all files).
      examples:  wk /s:*.tif tifzip {n}
                 wk /p:d:\photos /s:*.tif tifzip {n}
                 wk /q /o /p:.. /s:*.txt cmd /c type {P}
                 wk /p:d:\jbrekkie /s:*.jpg imgc {n} /o:out_{n} /l:2000
                 wk /o /s:*.jpg ic {N} /o:200_{N} /l:200
                 wk /d /p:.. /s:*lee* pv
                 wk /d /f /o git pull
      notes:     Optionally: use {B} as the target file's Basename (e.g. foo)
                                 {N} as the target file's Name (e.g. foo.txt)
                                 {P} as the target file's Path (e.g. c:\x\foo.txt)
                                 {D} as the target file's Directory (e.g. c:\x\)
                 Arguments /f, /o, /s, and /p must come before [command], or they are passed to the command.
                 Processes are started with the Current Working Directy that of the target file.
                 Process are run windowless, and their Standard Out is redirected to wk.exe.
                 Other process streams aren't redirected and are unavailable (StdIn, StdErr).
