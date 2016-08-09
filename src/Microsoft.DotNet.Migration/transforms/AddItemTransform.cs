﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Microsoft.DotNet.ProjectJsonMigration.Models;

namespace Microsoft.DotNet.ProjectJsonMigration.Transforms
{
    public class AddItemTransform<T> : ConditionalTransform<T>
    {
        private string _itemName;
        private string _includeValue;
        private string _excludeValue;

        private Func<T, string> _includeValueFunc;
        private Func<T, string> _excludeValueFunc;
        
        private bool _mergeExisting;

        private List<ItemMetadataValue<T>> _metadata = new List<ItemMetadataValue<T>>();

        public AddItemTransform(
            string itemName,
            IEnumerable<string> includeValues,
            IEnumerable<string> excludeValues,
            Func<T, bool> condition,
            bool mergeExisting = false)
            : this(itemName, string.Join(";", includeValues), string.Join(";", excludeValues), condition, mergeExisting) { }

        public AddItemTransform(
            string itemName,
            Func<T, string> includeValueFunc,
            Func<T, string> excludeValueFunc,
            Func<T, bool> condition,
            bool mergeExisting = false)
            : base(condition)
        {
            _itemName = itemName;
            _includeValueFunc = includeValueFunc;
            _excludeValueFunc = excludeValueFunc;
            _mergeExisting = mergeExisting;
        }

        public AddItemTransform(
            string itemName,
            string includeValue,
            Func<T, string> excludeValueFunc,
            Func<T, bool> condition,
            bool mergeExisting = false)
            : base(condition)
        {
            _itemName = itemName;
            _includeValue = includeValue;
            _excludeValueFunc = excludeValueFunc;
            _mergeExisting = mergeExisting;
        }

        public AddItemTransform(
            string itemName,
            Func<T, string> includeValueFunc,
            string excludeValue,
            Func<T, bool> condition,
            bool mergeExisting = false)
            : base(condition)
        {
            _itemName = itemName;
            _includeValueFunc = includeValueFunc;
            _excludeValue = excludeValue;
            _mergeExisting = mergeExisting;
        }

        public AddItemTransform(
            string itemName,
            string includeValue,
            string excludeValue,
            Func<T, bool> condition,
            bool mergeExisting=false)
            : base(condition)
        {
            _itemName = itemName;
            _includeValue = includeValue;
            _excludeValue = excludeValue;
            _mergeExisting = mergeExisting;
        }

        public AddItemTransform<T> WithMetadata(string metadataName, string metadataValue)
        {
            _metadata.Add(new ItemMetadataValue<T>(metadataName, metadataValue));
            return this;
        }

        public AddItemTransform<T> WithMetadata(string metadataName, Func<T, string> metadataValueFunc)
        {
            _metadata.Add(new ItemMetadataValue<T>(metadataName, metadataValueFunc));
            return this;
        }

        public AddItemTransform<T> WithMetadata(ItemMetadataValue<T> metadata)
        {
            _metadata.Add(metadata);
            return this;
        }

        public override void ConditionallyExecute(T source, ProjectRootElement destinationProject, ProjectElement destinationElement = null)
        {
            string includeValue = _includeValue ?? _includeValueFunc(source);
            string excludeValue = _excludeValue ?? _excludeValueFunc(source);

            var item = FindExistingItem(destinationProject, includeValue);
            if (item != default(ProjectItemElement) && !_mergeExisting)
            {
                throw new Exception("Existing item found, expected no item. Set mergeExisting to merge new metadata with existing items.");
            }

            // There is an existing item, merge exclude + metadata, with non-existing metadata being preferred in case of conflict
            if (item != default(ProjectItemElement))
            {
                excludeValue += item.Exclude;
                
                foreach (var existingMetadata in item.Metadata)
                {
                    _metadata.Insert(0, new ItemMetadataValue<T>(existingMetadata.Name, existingMetadata.Value));
                }

                item.Parent.RemoveChild(item);
            }

            ProjectItemGroupElement itemGroup = (ProjectItemGroupElement)destinationElement
                ?? destinationProject.AddItemGroup();

            var item = itemGroup.AddItem(_itemName, includeValue);
            item.Exclude = excludeValue;

            foreach (var metadata in _metadata)
            {
                item.AddMetadata(metadata.MetadataName, metadata.GetMetadataValue(source));
            }
        }

        private ProjectItemElement FindExistingItem(ProjectRootElement destinationProject, string includeValue)
        {
            return destinationProject.Items
                .Where(item => string.Equals(item.Include, includeValue, StringComparison.Ordinal))
                .FirstOrDefault();
        }
    }
}
