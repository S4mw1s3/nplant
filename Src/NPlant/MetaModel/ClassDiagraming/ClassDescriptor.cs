﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Reflection;
using NPlant.Core;
using NPlant.Generation.ClassDiagraming;

namespace NPlant.MetaModel.ClassDiagraming
{
    public abstract class ClassDescriptor : IKeyedItem
    {
        protected internal readonly IDictionary<string, bool> MemberVisibility = new Dictionary<string, bool>();
        private readonly KeyedList<ClassMemberDescriptor> _members = new KeyedList<ClassMemberDescriptor>();

        protected ClassDescriptor(Type reflectedType)
        {
            this.RenderInheritance = true;
            this.ReflectedType = reflectedType;
            this.Name = this.ReflectedType.GetFriendlyGenericName();
        }

        public void Visit()
        {
            var context = ClassDiagramVisitorContext.Current;
            this.MetaModel = context.GetTypeMetaModel(this.ReflectedType);

            LoadMembers(context);

            var showInheritance = ShouldShowInheritance(context);

            if (!this.MetaModel.Hidden)
            {
                foreach (ClassMemberDescriptor member in this.Members.InnerList)
                {
                    if (this.MetaModel.TreatAllMembersAsPrimitives)
                        member.TreatAsPrimitive = true;

                    TypeMetaModel metaModel = member.MetaModel;

                    if (!metaModel.Hidden && !member.TreatAsPrimitive)
                    {
                        // if not showing inheritance then show all members
                        // otherwise, only show member that aren't inherited
                        if (!showInheritance || !member.IsInherited)
                        {
                            if (metaModel.IsComplexType && this.GetMemberVisibility(member.Key))
                            {
                                var nextLevel = this.Level + 1;

                                if (member.MemberType.IsEnumerable())
                                {
                                    var enumeratorType = member.MemberType.GetEnumeratorType();
                                    var enumeratorTypeMetaModel = context.GetTypeMetaModel(enumeratorType);

                                    if (enumeratorTypeMetaModel.IsComplexType)
                                    {
                                        context.AddRelated(this, enumeratorType.GetReflected(), ClassDiagramRelationshipTypes.HasMany, nextLevel, member.Name);
                                    }
                                }
                                else
                                {
                                    context.AddRelated(this, member.MemberType.GetReflected(), ClassDiagramRelationshipTypes.HasA, nextLevel, member.Name);
                                }
                            }
                        }
                    }
                }
            }

            if (showInheritance)
            {
                context.AddRelated(this, this.ReflectedType.BaseType.GetReflected(), ClassDiagramRelationshipTypes.Base, this.Level - 1);
            }
        }

        private bool ShouldShowInheritance(ClassDiagramVisitorContext context)
        {
            bool showInheritance = this.RenderInheritance && this.ReflectedType.BaseType != null;

            if (showInheritance)
            {
                var baseTypeMetaModel = context.GetTypeMetaModel(this.ReflectedType.BaseType);

                showInheritance = !baseTypeMetaModel.HideAsBaseClass && !baseTypeMetaModel.Hidden;
            }

            return showInheritance;
        }

        protected virtual void LoadMembers(ClassDiagramVisitorContext context)
        {
            switch (context.ScanMode)
            {
                case ClassDiagramScanModes.SystemServiceModelMember:
                    _members.AddRange(this.ReflectedType.GetFields()
                                                        .Where(x => x.HasAttribute<DataMemberAttribute>() || x.HasAttribute<MessageBodyMemberAttribute>())
                                                        .Select(field => new ClassMemberDescriptor(this, field))
                                     );
                    _members.AddRange(this.ReflectedType.GetProperties()
                                                        .Where(x => x.HasAttribute<DataMemberAttribute>() || x.HasAttribute<MessageBodyMemberAttribute>())
                                                        .Select(property => new ClassMemberDescriptor(this, property))
                                     );
                    break;
                case ClassDiagramScanModes.AllMembers:
                    _members.AddRange(this.ReflectedType.GetFields(BindingFlags.Instance |
                                                                   BindingFlags.Public | 
                                                                   BindingFlags.NonPublic)
                                                        .Select(field => new ClassMemberDescriptor(this, field))
                                     );
                    _members.AddRange(this.ReflectedType.GetProperties(BindingFlags.Instance |
                                                                       BindingFlags.Public |
                                                                       BindingFlags.NonPublic)
                                                        .Select(property => new ClassMemberDescriptor(this, property))
                                     );
                    break;
                default:
                    _members.AddRange(this.ReflectedType.GetFields()
                                                        .Select(field => new ClassMemberDescriptor(this, field))
                                     );
                    _members.AddRange(this.ReflectedType.GetProperties()
                                                        .Select(property => new ClassMemberDescriptor(this, property))
                                     );
                    break;
            }
        }

        string IKeyedItem.Key { get { return this.Name; } }

        public string Name { get; protected set; }

        public bool RenderInheritance { get; set; }

        public Type ReflectedType { get; private set; }

        public int Level { get; protected set; }
        
        public KeyedList<ClassMemberDescriptor> Members { get { return _members; }}

        public virtual bool GetMemberVisibility(string name)
        {
            bool visibility;

            if (MemberVisibility.TryGetValue(name, out visibility))
                return visibility;

            // default to visible (i.e. if no specification is present, assume visible)
            return true;
        }

        public TypeMetaModel MetaModel { get; private set; }

        public string Color { get; set; }

        public override int GetHashCode()
        {
            return this.ReflectedType.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            ClassDescriptor descriptor = obj as ClassDescriptor;

            if (descriptor == null)
                return false;

            return descriptor.ReflectedType == this.ReflectedType;
        }

        public abstract IDescriptorWriter GetWriter(ClassDiagram diagram);
    }
}