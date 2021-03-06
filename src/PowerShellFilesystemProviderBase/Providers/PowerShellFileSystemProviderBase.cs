﻿using PowerShellFilesystemProviderBase.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace PowerShellFilesystemProviderBase.Providers
{
    public partial class PowerShellFileSystemProviderBase : NavigationCmdletProvider
    {
        private PowershellFileSystemDriveInfo DriveInfo => (PowershellFileSystemDriveInfo)this.PSDriveInfo;

        public (bool exists, ProviderNode? node) TryGetChildNode(ContainerNode parentNode, string childName)
        {
            var childNode = parentNode
                .GetChildItems()
                .FirstOrDefault(n => StringComparer.OrdinalIgnoreCase.Equals(n.Name, childName));

            return (childNode is not null, childNode);
        }

        protected bool TryGetNodeByPath(string path, [NotNullWhen(returnValue: true)] out ProviderNode? pathNode)
            => this.TryGetNodeByPath(new PathTool().SplitProviderPath(path).path.items, out pathNode);

        protected bool TryGetNodeByPath(string[] path, [NotNullWhen(returnValue: true)] out ProviderNode? pathNode)
        {
            pathNode = default;
            (bool exists, ProviderNode? node) traversal = (true, this.DriveInfo.RootNode);
            foreach (var pathItem in path)
            {
                traversal = traversal.node switch
                {
                    ContainerNode container => this.TryGetChildNode(container, pathItem),
                    _ => (false, default)
                };
                if (!traversal.exists) return false;
            }
            pathNode = traversal.node;
            return true;
        }

        protected bool TryGetNodeByPath<T>(ProviderNode startNode, string[] path, [NotNullWhen(returnValue: true)] out ProviderNode? providerNode, [NotNullWhen(returnValue: true)] out T? providerNodeCapbility) where T : class
        {
            providerNode = default;
            providerNodeCapbility = default;

            (bool exists, ProviderNode? node) cursor = (true, startNode);
            foreach (var pathItem in path)
            {
                cursor = cursor.node switch
                {
                    ContainerNode container => this.TryGetChildNode(container, pathItem),
                    _ => (false, default)
                };
                if (!cursor.exists) return false;
            }

            providerNode = cursor.node;
            // check if the underlying of the provider implemments the required capability
            providerNodeCapbility = cursor.node.Underlying as T;
            return providerNodeCapbility is not null;
        }

        protected ProviderNode GetDeepestNodeByPath(string path, out string[] missingPath)
           => this.GetDeepestNodeByPath(this.DriveInfo.RootNode, new PathTool().Split(path), out missingPath);

        protected ProviderNode GetDeepestNodeByPath(ProviderNode startNode, string[] path, out string[] missingPath)
        {
            var traversal = this.TraversePathComplete(startNode, path).ToArray();

            // the whole path might not exists
            missingPath = traversal.SkipWhile(t => t.exists).Select(t => t.name).ToArray();

            // at least the start node exists
            return traversal.TakeWhile(t => t.node is not null).LastOrDefault().node ?? startNode;
        }

        protected IEnumerable<(string name, bool exists, ProviderNode? node)> TraversePathComplete(ProviderNode startNode, string[] path)
        {
            (bool exists, ProviderNode? node) cursor = (true, startNode);

            foreach (var pathItem in path)
            {
                if (cursor.exists)
                {
                    // try to descend
                    cursor = cursor.node switch
                    {
                        // you can only fetch child node from containers
                        ContainerNode c => this.TryGetChildNode(c, pathItem),

                        // from leaf nodes, no child can be retrieved
                        _ => (false, null)
                    };

                    // the result of the last traversal is returned
                    yield return (
                        name: pathItem,
                        exists: cursor.exists,
                        node: cursor.node
                    );
                }
                else
                {
                    // path points where new node exists
                    yield return (
                        name: pathItem,
                        exists: false,
                        node: null
                    );
                }
            }
        }

        #region Write a ProviderNode to pipeline

        protected void WriteProviderNode(string path, ProviderNode node)
        {
            // PowerShell would wrap the underlying with a PSObject itself
            // but taling the PSObject from the node allows to provide additionel properties
            var psobject = node.GetItem();
            if (psobject is null)
                return;

            this.WriteItemObject(
                item: psobject,
                path: this.DecoratePath(path),
                isContainer: node is ContainerNode);
        }

        protected string DecoratePath(string path) => @$"{this.PSDriveInfo.Name}:\{path}";

        #endregion Write a ProviderNode to pipeline

        #region Invoke a node capability

        /// <summary>
        /// Invokes a member of <see cref="ContainerNode"/>. If the node isn't a container the fallback is invoked
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="invoke"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        protected T InvokeContainerNodeOrDefault<T>(string[] path, Func<ContainerNode, T> invoke, Func<T> fallback)
        {
            if (this.TryGetContainerNodeByPath(path, out var containerNode))
            {
                return invoke(containerNode);
            }
            else return fallback();
        }

        protected void InvokeContainerNodeOrDefault(string[] path, Action<ContainerNode> invoke, Action fallback)
        {
            if (this.TryGetContainerNodeByPath(path, out var containerNode))
            {
                invoke(containerNode);
            }
            else fallback();
        }

        protected bool TryGetContainerNodeByPath(string[] path, [NotNullWhen(true)] out ContainerNode? containerNode)
        {
            if (this.TryGetNodeByPath(path, out var providerNode))
            {
                if (providerNode is ContainerNode container)
                {
                    containerNode = container;
                    return true;
                }

                containerNode = default;
                return false;
            }
            else throw new ItemNotFoundException($"Can't find path '{string.Join(this.ItemSeparator, path)}'");
        }

        protected void InvokeProviderNodeOrDefault(string[] path, Action<ProviderNode> invoke, Action fallback)
        {
            if (this.TryGetNodeByPath(path, out var node))
            {
                invoke(node);
            }
            else fallback();
        }

        protected T InvokeProviderNodeOrDefault<T>(string[] path, Func<ProviderNode, T> invoke, Func<T> fallback)
        {
            if (this.TryGetNodeByPath(path, out var node))
            {
                return invoke(node);
            }
            else return fallback();
        }

        #endregion Invoke a node capability
    }
}