﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

namespace Discord.Commands
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class ModuleInfo
    {
        internal readonly Func<ModuleBase> _builder;

        public TypeInfo Source { get; }
        public CommandService Service { get; }
        public string Name { get; }
        public string Prefix { get; }
        public string Summary { get; }
        public string Remarks { get; }
        public IEnumerable<CommandInfo> Commands { get; }
        public IReadOnlyList<PreconditionAttribute> Preconditions { get; }

        internal ModuleInfo(TypeInfo source, CommandService service, IDependencyMap dependencyMap)
        {
            Source = source;
            Service = service;
            Name = source.Name;
            _builder = ReflectionUtils.CreateBuilder<ModuleBase>(source, Service, dependencyMap);

            var groupAttr = source.GetCustomAttribute<GroupAttribute>();
            if (groupAttr != null)
                Prefix = groupAttr.Prefix;
            else
                Prefix = "";

            var nameAttr = source.GetCustomAttribute<NameAttribute>();
            if (nameAttr != null)
                Name = nameAttr.Text;

            var summaryAttr = source.GetCustomAttribute<SummaryAttribute>();
            if (summaryAttr != null)
                Summary = summaryAttr.Text;

            var remarksAttr = source.GetCustomAttribute<RemarksAttribute>();
            if (remarksAttr != null)
                Remarks = remarksAttr.Text;

            List<CommandInfo> commands = new List<CommandInfo>();
            SearchClass(source, commands, Prefix, dependencyMap);
            Commands = commands;

            Preconditions = Source.GetCustomAttributes<PreconditionAttribute>().ToImmutableArray();
        }
        private void SearchClass(TypeInfo parentType, List<CommandInfo> commands, string groupPrefix, IDependencyMap dependencyMap)
        {
            foreach (var method in parentType.DeclaredMethods)
            {
                var cmdAttr = method.GetCustomAttribute<CommandAttribute>();
                if (cmdAttr != null)
                    commands.Add(new CommandInfo(method, this, cmdAttr, groupPrefix));
            }
            foreach (var type in parentType.DeclaredNestedTypes)
            {
                var groupAttrib = type.GetCustomAttribute<GroupAttribute>();
                if (groupAttrib != null)
                {
                    string nextGroupPrefix;

                    if (groupPrefix != "")
                        nextGroupPrefix = groupPrefix + " " + (groupAttrib.Prefix ?? type.Name.ToLowerInvariant());
                    else
                        nextGroupPrefix = groupAttrib.Prefix ?? type.Name.ToLowerInvariant();

                    SearchClass(type, commands, nextGroupPrefix, dependencyMap);
                }
            }
        }

        internal ModuleBase CreateInstance()
            => _builder();

        public override string ToString() => Name;
        private string DebuggerDisplay => Name;
    }
}