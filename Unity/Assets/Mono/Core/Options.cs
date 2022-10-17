using CommandLine;
using System;
using System.Collections.Generic;

namespace ET
{
    public enum AppType
    {
        Server,
        Watcher, // 每台物理机一个守护进程，用来启动该物理机上的所有进程
        GameTool,
        ExcelExporter,
        Proto2CS,
        Robot,      //CA96303F notes: 不加这个的话, Watcher 拉不起来 Robot
    }
    
    public class Options
    {
        public static Options Instance { get; set; }

                    //CA96303F 把这里的 Default = AppType.Server 改为 Watcher 试看看, 能发现本机拉起了 3 个进程: Watcher, Server, Robot, 都在 xlsx 里配置着。 
        [Option("AppType", Required = false, Default = AppType.Server, HelpText = "AppType enum")]
        public AppType AppType { get; set; }

        [Option("Process", Required = false, Default = 1)]
        public int Process { get; set; } = 1;
        
        [Option("Develop", Required = false, Default = 0, HelpText = "develop mode, 0正式 1开发 2压测")]
        public int Develop { get; set; } = 0;

        [Option("LogLevel", Required = false, Default = 2)]
        public int LogLevel { get; set; } = 2;
        
        [Option("Console", Required = false, Default = 0)]
        public int Console { get; set; } = 0;

        [Option("StartConfig", Required = false, Default = ".")] // CA96303F 这里 default 原本是 "", 我改成了 "." 否则 watcher 拉不起来 server app 进程
        public string StartConfig { get; set; } = "";
        
        // 进程启动是否创建该进程的scenes
        [Option("CreateScenes", Required = false, Default = 1)]
        public int CreateScenes { get; set; } = 1;
    }
}