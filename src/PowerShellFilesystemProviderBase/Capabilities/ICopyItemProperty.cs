﻿using PowerShellFilesystemProviderBase.Nodes;
using System.Management.Automation;

namespace PowerShellFilesystemProviderBase.Capabilities
{
    public interface ICopyItemProperty
    {
        /// <summary>
        /// Returns custom parameters to be applied for the copying of item properties in
        /// </summary>
        /// <param name="properties"></param>
        /// <returns>empty <see cref="RuntimeDefinedParameterDictionary"/> by default</returns>
        object? CopyItemPropertyParameters(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty) => new RuntimeDefinedParameterDictionary();

        /// <summary>
        /// Copy the given item property
        /// </summary>
        /// <param name="properties"></param>
        void CopyItemProperty(ProviderNode sourceNode, string sourcePropertyName, string destinationPropertyName);
    }
}