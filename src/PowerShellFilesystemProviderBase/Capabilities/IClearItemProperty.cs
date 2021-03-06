﻿using System.Collections.Generic;
using System.Management.Automation;

namespace PowerShellFilesystemProviderBase.Capabilities
{
    public interface IClearItemProperty
    {
        /// <summary>
        /// Returns custom parameters to be applied for the clearing an the item property name <paramref name="propertyToClear"/>
        /// </summary>
        /// <param name="propertyToClear"></param>
        /// <returns>empty <see cref="RuntimeDefinedParameterDictionary"/> by default</returns>
        object? ClearItemPropertyParameters(IEnumerable<string> propertyToClear) => new RuntimeDefinedParameterDictionary();

        /// <summary>
        /// Removes the value from an item property.
        /// </summary>
        /// <param name="propertyToClear"></param>
        void ClearItemProperty(IEnumerable<string> propertyToClear);
    }
}