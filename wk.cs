using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;

class Walk
{
    static void Usage()
    {
        Console.WriteLine( @"Usage: wk [/d] [/e] [/f] [/o] [/p:<path>] [/s:<filespec>] command" );
        Console.WriteLine( @"  Walk:      Recursively walk the filesystem executing commands on files or directories." );
        Console.WriteLine( @"  arguments: command     The command invoked." );
        Console.WriteLine( @"             /d          Walk directories, not files. Executes command in each directory." );
        Console.WriteLine( @"             /e          Display errors when folders/files can't be enumerated. Probably access denied." );
        Console.WriteLine( @"             /f          Flat. Process files (or folders with /d) in [path] and don't recurse." );
        Console.WriteLine( @"             /o          One process runs at a time. Default is based on core count." );
        Console.WriteLine( @"             /p          The root path for processing. Default is .\ (the current directory)." );
        Console.WriteLine( @"             /q          Quiet; only generate output from child processes. Overrides /e" );
        Console.WriteLine( @"             /s          The files for which command is invoked. Default is *.* (all files)." );
        Console.WriteLine( @"  examples:  wk /s:*.tif tifzip {n}" );
        Console.WriteLine( @"             wk /p:d:\photos /s:*.tif tifzip {n}" );
        Console.WriteLine( @"             wk /q /o /p:.. /s:*.txt cmd /c type {P}" );
        Console.WriteLine( @"             wk /p:d:\jbrekkie /s:*.jpg imgc {n} /o:out_{n} /l:2000" );
        Console.WriteLine( @"             wk /o /s:*.jpg ic {N} /o:200_{N} /l:200" );
        Console.WriteLine( @"             wk /d /p:.. /s:*lee* pv" );
        Console.WriteLine( @"  notes:     Optionally: use {B} as the target file's Basename (e.g. foo)" );
        Console.WriteLine( @"                             {N} as the target file's Name (e.g. foo.txt)" );
        Console.WriteLine( @"                             {P} as the target file's Path (e.g. c:\x\foo.txt)" );
        Console.WriteLine( @"                             {D} as the target file's Directory (e.g. c:\x\)" );
        Console.WriteLine( @"             Arguments /f, /o, /s, and /p must come before [command], or they are passed to the command." );
        Console.WriteLine( @"             Processes are started with the Current Working Directy that of the target file." );
        Console.WriteLine( @"             Process are run windowless, and their Standard Out is redirected to wk.exe." );
        Console.WriteLine( @"             Other process streams aren't redirected and are unavailable (StdIn, StdErr)." );

        Environment.Exit( 1 );
    } //Usage

    class EnumeratedItem
    {
        static object lockObj = new object();
        string arguments, command;
        bool quiet, showErrors;

        public EnumeratedItem( string args, string cmd, bool q, bool errors )
        {
            arguments = args;
            command = cmd;
            quiet = q;
            showErrors = errors;
        }

        void ExecuteProc( string name, string fullname, string parentfullname, string workingdirectory )
        {
            try
            {
                string justName = name;
                if ( justName.Contains( ' ' ) )
                    justName = "\"" + justName + "\"";
    
                string fullPath = fullname;
                if ( fullPath.Contains( ' ' ) )
                    fullPath = "\"" + fullPath + "\"";
    
                string fullDirectory = parentfullname;
                if ( fullDirectory.Contains( ' ' ) )
                    fullDirectory = "\"" + fullDirectory + "\"";
    
                string baseName = Path.GetFileNameWithoutExtension( name );
                if ( baseName.Contains( ' ' ) )
                    baseName = "\"" + baseName + "\"";
        
                string a = arguments;
                if ( null != a )
                {
                    a = a.Replace( "{b}", baseName );
                    a = a.Replace( "{d}", fullDirectory );
                    a = a.Replace( "{n}", justName );
                    a = a.Replace( "{p}", fullPath );
                }
    
                ProcessStartInfo startInfo = new ProcessStartInfo( command, a );
                startInfo.WorkingDirectory = workingdirectory;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
    
                Process proc = Process.Start( startInfo );
    
                string output = proc.StandardOutput.ReadToEnd();
    
                lock ( lockObj )
                {
                    if ( !quiet )
                        Console.WriteLine( "ran {0} {1} in directory {2}", command, a, workingdirectory );

                    if ( 0 != output.Length )
                        Console.Write( output );
                }
    
                proc.WaitForExit();
                proc.Close();
            }
            catch( Exception ex )
            {
                if ( showErrors )
                    Console.WriteLine( "exception {0} processing {1}", ex.ToString(), fullname );
            }
        } //ExecuteProc

        public void HandleDirectory( DirectoryInfo di )
        {
            ExecuteProc( di.Name, di.FullName, di.Parent.FullName, di.FullName );
        }

        public void HandleFile( FileInfo fi )
        {
            ExecuteProc( fi.Name, fi.FullName, fi.DirectoryName, fi.DirectoryName );
        }
    } //EnumeratedItem

    static void Main( string[] args )
    {
        if ( args.Count() < 1 )
            Usage();

        string path = @".\";
        string filespec = @"*.*";
        string command = null;
        string arguments = null;
        bool pastCommand = false;
        bool oneProc = false;
        bool recurse = true;
        bool quiet = false;
        bool showErrors = false;
        bool walkDirectories = false;

        for ( int i = 0; i < args.Length; i++ )
        {
            if ( !pastCommand && ( '-' == args[i][0] || '/' == args[i][0] ) )
            {
                string argUpper = args[i].ToUpper();
                string arg = args[i];
                char c = argUpper[1];

                if ( 'D' == c )
                    walkDirectories = true;
                else if ( 'S' == c )
                {
                    if ( arg[2] != ':' )
                        Usage();

                    filespec = arg.Substring( 3 );
                }
                else if ( 'P' == c )
                {
                    if ( arg[2] != ':' )
                        Usage();

                    path = arg.Substring( 3 );
                }
                else if ( 'O' == c )
                    oneProc = true;
                else if ( 'F' == c )
                    recurse = false;
                else if ( 'Q' == c )
                    quiet = true;
                else if ( 'E' == c )
                    showErrors = true;
                else
                    Usage();
            }
            else
            {
                pastCommand = true;

                if ( null == command )
                    command = args[ i ];
                else if ( null == arguments )
                    arguments = args[ i ];
                else
                    arguments += " " + args[ i ];
            }
        }

        if ( null == command )
        {
            Console.WriteLine( "no command specified" );
            Usage();
        }

        if ( quiet )
            showErrors = false;

        if ( null != arguments )
        {
            arguments.Replace( "{B}", "{b}" );
            arguments.Replace( "{D}", "{d}" );
            arguments.Replace( "{N}", "{n}" );
            arguments.Replace( "{P}", "{p}" );
        }

        path = Path.GetFullPath( path );
        DirectoryInfo diRoot = new DirectoryInfo( path );
        EnumeratedItem item = new EnumeratedItem( arguments, command, quiet, showErrors );
        ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = oneProc ? 1 : -1 };

        if ( walkDirectories )
            item.HandleDirectory( diRoot );

        EnumerateDirectory( diRoot, filespec, item, recurse, po, walkDirectories );
    } //Main

    static void EnumerateDirectory( DirectoryInfo diRoot, string filePattern, EnumeratedItem item, bool recurse, ParallelOptions opts, bool enumDirs )
    {
        if ( recurse )
        {
            try
            {
                Parallel.ForEach( diRoot.EnumerateDirectories(), opts, ( directoryInfo ) =>
                {
                    EnumerateDirectory( directoryInfo, filePattern, item, recurse, opts, enumDirs );
                });
            }
            catch ( Exception ex )
            {
                //Console.WriteLine( "exception {0} enumerating folders", ex.ToString() );
            }
        }

        try
        {
            if ( enumDirs )
            {
                Parallel.ForEach( diRoot.EnumerateDirectories( filePattern ), opts, ( directoryInfo ) =>
                {
                    item.HandleDirectory( directoryInfo );
                });
            }
            else
            {
                Parallel.ForEach( diRoot.EnumerateFiles( filePattern ), opts, ( fileInfo ) =>
                {
                    item.HandleFile( fileInfo );
                });
            }
        }
        catch ( Exception ex )
        {
            //Console.WriteLine( "exception {0} getting items from dir\n", ex.ToString() );
        }
    } //EnumerateDirectory
} //Walk
