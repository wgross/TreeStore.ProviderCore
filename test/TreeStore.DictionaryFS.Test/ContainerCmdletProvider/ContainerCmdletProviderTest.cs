﻿using Moq;
using PowerShellFilesystemProviderBase;
using PowerShellFilesystemProviderBase.Capabilities;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Xunit;
using UnderlyingDictionary = System.Collections.Generic.Dictionary<string, object?>;

namespace TreeStore.DictionaryFS.Test.ContainerCmdletProvider
{
    [Collection(nameof(PowerShell))]
    public sealed class ContainerCmdletProviderTest : ContainerCmdletProviderTestBase
    {
        #region Get-ChildItem -Path -Recurse

        [Fact]
        public void Powershell_retrieves_roots_childnodes()
        {
            // ARRANGE
            var root = new UnderlyingDictionary
            {
                ["child1"] = new UnderlyingDictionary { },
                ["property"] = "text"
            };

            this.ArrangeFileSystem(root);

            // ACT
            var result = this.PowerShell.AddCommand("Get-ChildItem")
                .AddParameter("Path", @"test:\")
                .Invoke()
                .ToArray();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);
            Assert.Single(result);

            var psobject = result.ElementAt(0);

            Assert.Equal("child1", psobject.Property<string>("PSChildName"));
            Assert.True(psobject.Property<bool>("PSIsContainer"));
            Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
            Assert.Equal("TestFilesystem", psobject.Property<ProviderInfo>("PSProvider").Name);
            Assert.Equal(@"TestFileSystem\TestFilesystem::test:\child1", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TestFileSystem\TestFilesystem::test:\", psobject.Property<string>("PSParentPath"));
        }

        [Fact]
        public void Powershell_retrieves_roots_childnodes_recursive()
        {
            // ARRANGE
            var root = new UnderlyingDictionary
            {
                ["child1"] = new UnderlyingDictionary
                {
                    ["grandchild"] = new UnderlyingDictionary()
                    {
                        ["property"] = "text"
                    }
                }
            };

            this.ArrangeFileSystem(root);

            // ACT
            var result = this.PowerShell.AddCommand("Get-ChildItem")
                .AddParameter("Path", @"test:\")
                .AddParameter("Recurse")
                .Invoke()
                .ToArray();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);
            Assert.Equal(2, result.Count());

            var psobject = result.ElementAt(0);

            Assert.Equal("child1", psobject.Property<string>("PSChildName"));
            Assert.True(psobject.Property<bool>("PSIsContainer"));
            Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
            Assert.Equal("TestFilesystem", psobject.Property<ProviderInfo>("PSProvider").Name);
            Assert.Equal(@"TestFileSystem\TestFilesystem::test:\child1", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TestFileSystem\TestFilesystem::test:\", psobject.Property<string>("PSParentPath"));

            psobject = result.ElementAt(1);

            Assert.Equal("grandchild", psobject.Property<string>("PSChildName"));
            Assert.True(psobject.Property<bool>("PSIsContainer"));
            Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
            Assert.Equal("TestFilesystem", psobject.Property<ProviderInfo>("PSProvider").Name);
            Assert.Equal(@"TestFileSystem\TestFilesystem::test:\child1\grandchild", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TestFileSystem\TestFilesystem::test:\child1", psobject.Property<string>("PSParentPath"));
        }

        [Fact]
        public void Powershell_retrieves_roots_childnodes_recursive_upto_depth()
        {
            // ARRANGE
            var root = new UnderlyingDictionary
            {
                ["child1"] = new UnderlyingDictionary
                {
                    ["grandchild"] = new UnderlyingDictionary()
                    {
                        ["grandgrandchild"] = new UnderlyingDictionary()
                    }
                },
                ["property"] = "text",
                ["child2"] = Mock.Of<IItemContainer>()
            };

            this.ArrangeFileSystem(root);

            // ACT
            var result = this.PowerShell.AddCommand("Get-ChildItem")
                .AddParameter("Path", @"test:\")
                .AddParameter("Recurse")
                .AddParameter("Depth", 1) // only children, no grandchildren
                .Invoke()
                .ToArray();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);
            Assert.Equal(2, result.Count());

            var psobject = result.ElementAt(0);

            Assert.Equal("child1", psobject.Property<string>("PSChildName"));
            Assert.True(psobject.Property<bool>("PSIsContainer"));
            Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
            Assert.Equal("TestFilesystem", psobject.Property<ProviderInfo>("PSProvider").Name);
            Assert.Equal(@"TestFileSystem\TestFilesystem::test:\child1", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TestFileSystem\TestFilesystem::test:\", psobject.Property<string>("PSParentPath"));

            psobject = result.ElementAt(1);

            Assert.Equal("grandchild", psobject.Property<string>("PSChildName"));
            Assert.True(psobject.Property<bool>("PSIsContainer"));
            Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
            Assert.Equal("TestFilesystem", psobject.Property<ProviderInfo>("PSProvider").Name);
            Assert.Equal(@"TestFileSystem\TestFilesystem::test:\child1\grandchild", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TestFileSystem\TestFilesystem::test:\child1", psobject.Property<string>("PSParentPath"));
        }

        #endregion Get-ChildItem -Path -Recurse

        #region Remove-Item -Path -Recurse

        [Fact]
        public void Powershell_removes_root_child_node()
        {
            // ARRANGE
            var root = new UnderlyingDictionary
            {
                ["child1"] = new UnderlyingDictionary(),
            };

            this.ArrangeFileSystem(root);

            // ACT
            var result = this.PowerShell.AddCommand("Remove-Item")
                .AddParameter("Path", @"test:\child1")
                .Invoke()
                .ToArray();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);
            Assert.Empty(root);
        }

        [Fact]
        public void Powershell_removes_root_child_node_fails_if_node_has_children()
        {
            // ARRANGE
            var root = new UnderlyingDictionary
            {
                ["child1"] = new UnderlyingDictionary()
                {
                    ["grandchild1"] = new UnderlyingDictionary()
                }
            };

            this.ArrangeFileSystem(root);

            // ACT
            var result = Assert.Throws<CmdletInvocationException>(() => this.PowerShell
                .AddCommand("Remove-Item")
                .AddParameter("Path", @"test:\child1")
                .Invoke());

            // ASSERT
            Assert.True(this.PowerShell.HadErrors);
        }

        [Fact]
        public void Powershell_removes_root_child_node_recursive()
        {
            // ARRANGE
            var root = new UnderlyingDictionary
            {
                ["child1"] = new UnderlyingDictionary
                {
                    ["grandchild1"] = new UnderlyingDictionary()
                }
            };

            this.ArrangeFileSystem(root);

            // ACT
            var result = this.PowerShell.AddCommand("Remove-Item")
                .AddParameter("Path", @"test:\child1")
                .AddParameter("Recurse", true)
                .Invoke()
                .ToArray();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);
            Assert.Empty(root);
        }

        #endregion Remove-Item -Path -Recurse

        #region New-Item -Path -ItemType -Value

        [Fact]
        public void Powershell_creates_child_item()
        {
            // ARRANGE
            var root = new UnderlyingDictionary();
            var child = new UnderlyingDictionary();

            this.ArrangeFileSystem(root);

            // ACT
            var result = this.PowerShell.AddCommand("New-Item")
                .AddParameter("Path", @"test:\child1")
                .AddParameter("Value", child)
                .Invoke()
                .ToArray();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            var psobject = result.Single();

            Assert.Equal("child1", psobject.Property<string>("PSChildName"));
            Assert.True(psobject.Property<bool>("PSIsContainer"));
            Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
            Assert.Equal("TestFilesystem", psobject.Property<ProviderInfo>("PSProvider").Name);
            Assert.Equal(@"TestFileSystem\TestFilesystem::test:\child1", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TestFileSystem\TestFilesystem::test:\", psobject.Property<string>("PSParentPath"));

            Assert.True(root.TryGetValue("child1", out var added));
            Assert.Same(child, added);
        }

        #endregion New-Item -Path -ItemType -Value

        #region Rename-Item -Path -NewName

        [Fact]
        public void Powershell_renames_childitem()
        {
            // ARRANGE
            var child = new UnderlyingDictionary();
            var root = new UnderlyingDictionary
            {
                ["child1"] = child
            };

            this.ArrangeFileSystem(root);

            // ACT
            var result = this.PowerShell.AddCommand("Rename-Item")
                .AddParameter("Path", @"test:\child1")
                .AddParameter("NewName", "newName")
                .Invoke()
                .ToArray();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);
            Assert.True(root.TryGetValue("newName", out var renamed));
            Assert.Same(child, renamed);
        }

        #endregion Rename-Item -Path -NewName

        #region Copy-Item -Path -Destination -Recurse

        [Fact]
        public void Powershell_copies_child()
        {
            // ARRANGE
            var child1 = new UnderlyingDictionary()
            {
                ["child1"] = new UnderlyingDictionary()
            };

            var root = new UnderlyingDictionary
            {
                ["child1"] = child1,
                ["child2"] = new UnderlyingDictionary()
            };

            this.ArrangeFileSystem(root);

            // ACT
            var result = this.PowerShell.AddCommand("Copy-Item")
                .AddParameter("Path", @"test:\child1")
                .AddParameter("Destination", @"test:\child2")
                .Invoke()
                .ToArray();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);
            Assert.True(root.TryGetValue<UnderlyingDictionary>("child2", out var child2));
            Assert.True(child2!.TryGetValue<UnderlyingDictionary>("child1", out var copy_child1));
            Assert.NotNull(copy_child1!);
            Assert.NotSame(child1, copy_child1);
        }

        [Fact]
        public void Powershell_copy_child_with_new_name()
        {
            // ARRANGE
            var child1 = new UnderlyingDictionary()
            {
                ["child1"] = new UnderlyingDictionary()
            };

            var root = new UnderlyingDictionary
            {
                ["child1"] = child1,
                ["child2"] = new UnderlyingDictionary()
            };

            this.ArrangeFileSystem(root);

            // ACT
            var result = this.PowerShell.AddCommand("Copy-Item")
                .AddParameter("Path", @"test:\child1")
                .AddParameter("Destination", @"test:\child2\newname")
                .Invoke()
                .ToArray();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);
            Assert.True(root.TryGetValue<UnderlyingDictionary>("child2", out var child2));
            Assert.True(child2!.TryGetValue<UnderlyingDictionary>("newname", out var copy_child1));
            Assert.NotNull(copy_child1!);
            Assert.NotSame(child1, copy_child1);
        }

        [Fact]
        public void Powershell_copies_child_recursive()
        {
            // ARRANGE
            var child1 = new UnderlyingDictionary()
            {
                ["grandchild"] = new UnderlyingDictionary(),
                ["data"] = 1,
            };

            var root = new UnderlyingDictionary
            {
                ["child1"] = child1,
                ["child2"] = new UnderlyingDictionary()
            };

            this.ArrangeFileSystem(root);

            // ACT
            var result = this.PowerShell.AddCommand("Copy-Item")
                .AddParameter("Path", @"test:\child1")
                .AddParameter("Destination", @"test:\child2")
                .AddParameter("Recurse")
                .Invoke()
                .ToArray();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);
            Assert.True(root.TryGetValue<UnderlyingDictionary>("child2", out var child2));
            Assert.True(child2!.TryGetValue<UnderlyingDictionary>("child1", out var copy_child1));
            Assert.NotNull(copy_child1!);
            Assert.NotSame(child1, copy_child1);
            Assert.True(copy_child1!.TryGetValue<UnderlyingDictionary>("grandchild", out var copy_grandchild));
            Assert.True(copy_child1!.TryGetValue<int>("data", out var data));
            Assert.Equal(1, data);
        }

        [Fact]
        public void Powershell_copies_child_item_with_new_name_and_parent_recursive()
        {
            // ARRANGE
            var child1 = new UnderlyingDictionary()
            {
                ["grandchild"] = new UnderlyingDictionary(),
                ["data"] = 1,
            };

            var root = new UnderlyingDictionary
            {
                ["child1"] = child1,
                ["child2"] = new UnderlyingDictionary()
            };

            this.ArrangeFileSystem(root);

            // ACT
            var result = this.PowerShell.AddCommand("Copy-Item")
                .AddParameter("Path", @"test:\child1")
                .AddParameter("Destination", @"test:\child2\parent\newname")
                .AddParameter("Recurse")
                .Invoke()
                .ToArray();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);
            Assert.True(root.TryGetValue<UnderlyingDictionary>("child2", out var child2));
            Assert.True(child2!.TryGetValue<UnderlyingDictionary>("parent", out var parent));
            Assert.True(parent!.TryGetValue<UnderlyingDictionary>("newname", out var newname));

            Assert.NotNull(newname!);
            Assert.NotSame(child1, newname);
            Assert.True(newname!.TryGetValue<UnderlyingDictionary>("grandchild", out var copy_grandchild));
            Assert.True(newname!.TryGetValue<int>("data", out var data));
            Assert.Equal(1, data);
        }

        #endregion Copy-Item -Path -Destination -Recurse
    }
}