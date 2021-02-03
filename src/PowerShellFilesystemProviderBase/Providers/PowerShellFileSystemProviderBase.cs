﻿using PowerShellFilesystemProviderBase.Nodes;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace PowerShellFilesystemProviderBase.Providers
{
    public partial class PowerShellFileSystemProviderBase : ContainerCmdletProvider
    {
        private PowershellFileSystemDriveInfo DriveInfo => (PowershellFileSystemDriveInfo)this.PSDriveInfo;

        protected bool TryGetNodeByPath(string path, [NotNullWhen(returnValue: true)] out ProviderNode? pathNode)
        {
            pathNode = default;
            (bool exists, ProviderNode? node) traversal = (true, this.DriveInfo.RootNode);
            foreach (var pathItem in new PathTool().Split(path))
            {
                traversal = traversal.node switch
                {
                    ContainerNode r => r.TryGetChildNode(pathItem),
                    _ => (false, default)
                };
                if (!traversal.exists) return false;
            }
            pathNode = traversal.node;
            return true;
        }

        protected bool TryGetNodeByPath<T>(string path, [NotNullWhen(returnValue: true)] out T? pathNodeWithCapability)
        {
            if (this.TryGetNodeByPath(path, out var node))
            {
                if (node is T hasCapability)
                {
                    pathNodeWithCapability = hasCapability;
                    return true;
                }
            }
            pathNodeWithCapability = default;
            return false;
        }

        protected PSObject DecorateItem(string path, PSObject psobject)
        {
            psobject.Properties.Add(new PSNoteProperty("PSParentPath",
                @$"{this.PSDriveInfo.Provider.ModuleName}\{this.PSDriveInfo.Provider.Name}::{this.PSDriveInfo.Name}:\{Path.GetDirectoryName(path)}"));
            return psobject;
        }

        protected string DecoratePath(string path) => @$"{this.PSDriveInfo.Provider.ModuleName}\{this.PSDriveInfo.Provider.Name}::{this.PSDriveInfo.Name}:\{path}";
    }
}