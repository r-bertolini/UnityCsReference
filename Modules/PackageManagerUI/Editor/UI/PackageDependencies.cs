// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageDependencies : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDependencies> {}

        public PackageDependencies()
        {
            var root = Resources.GetTemplate("PackageDependencies.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            SetExpanded(PackageManagerPrefs.instance.dependenciesExpanded);
            dependenciesExpander.RegisterValueChangedCallback(evt => SetExpanded(evt.newValue));

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            float newWidth = evt.newRect.width;
            ToggleLowWidthDependencyView(newWidth);
        }

        private void ToggleLowWidthDependencyView(float width)
        {
            if (width <= 420)
            {
                UIUtils.SetElementDisplay(dependenciesListLowWidth, true);
                UIUtils.SetElementDisplay(reverseDependenciesListLowWidth, true);
                UIUtils.SetElementDisplay(dependenciesNormalView, false);
                UIUtils.SetElementDisplay(reverseDependenciesNormalView, false);
            }
            else
            {
                UIUtils.SetElementDisplay(dependenciesListLowWidth, false);
                UIUtils.SetElementDisplay(reverseDependenciesListLowWidth, false);
                UIUtils.SetElementDisplay(dependenciesNormalView, true);
                UIUtils.SetElementDisplay(reverseDependenciesNormalView, true);
            }
        }

        private void SetExpanded(bool expanded)
        {
            if (dependenciesExpander.value != expanded)
                dependenciesExpander.value = expanded;
            if (PackageManagerPrefs.instance.dependenciesExpanded != expanded)
                PackageManagerPrefs.instance.dependenciesExpanded = expanded;
            UIUtils.SetElementDisplay(dependenciesOuterContainer, expanded);
        }

        private static Label BuildLabel(string text, string clazz)
        {
            var label = new Label(text);
            label.AddToClassList(clazz);
            label.tooltip = text;
            return label;
        }

        private static TextField BuildTextField(string text, string clazz)
        {
            var textfield = new TextField();
            textfield.SetValueWithoutNotify(text);
            textfield.AddToClassList(clazz);
            textfield.tooltip = text;
            textfield.isReadOnly = true;
            return textfield;
        }

        private static string GetNameText(DependencyInfo dependency)
        {
            var packageVersion = PackageDatabase.instance.GetPackageVersion(dependency);
            return packageVersion != null ? packageVersion.displayName : UpmPackageVersion.ExtractDisplayName(dependency.name);
        }

        private static string GetStatusText(DependencyInfo dependency)
        {
            var installedVersion = PackageDatabase.instance.GetPackage(dependency.name)?.versions.installed;
            if (installedVersion == null)
                return string.Empty;

            if (installedVersion.HasTag(PackageTag.Custom))
                return ApplicationUtil.instance.GetTranslationForText("(custom)");

            if (installedVersion.HasTag(PackageTag.Local))
                return ApplicationUtil.instance.GetTranslationForText("(local)");

            var statusText = installedVersion.HasTag(PackageTag.BuiltIn)
                ? ApplicationUtil.instance.GetTranslationForText("enabled") : ApplicationUtil.instance.GetTranslationForText("installed");
            return installedVersion.version?.ToString() == dependency.version
                ? string.Format("({0} \u2714)", statusText) : string.Format("({0} {1} \u2714)", installedVersion.version, statusText);
        }

        public void SetPackageVersion(IPackageVersion version)
        {
            var dependencies = version?.dependencies;
            var reverseDependencies = PackageDatabase.instance.GetReverseDependencies(version);
            var showDependency = PackageManagerProjectSettings.instance.enablePackageDependencies && (dependencies != null || reverseDependencies != null);
            UIUtils.SetElementDisplay(this, showDependency);
            if (!showDependency)
                return;

            UpdateDependencies(dependencies);
            UpdateReverseDependencies(reverseDependencies);
        }

        private void UpdateDependencies(DependencyInfo[] dependencies)
        {
            dependenciesNames.Clear();
            dependenciesVersions.Clear();
            dependenciesStatuses.Clear();
            dependenciesListLowWidth.Clear();

            var hasDependencies = dependencies?.Any() ?? false;
            UIUtils.SetElementDisplay(noDependencies, !hasDependencies);
            UIUtils.SetElementDisplay(dependenciesNames, hasDependencies);
            UIUtils.SetElementDisplay(dependenciesVersions, hasDependencies);

            if (!hasDependencies)
                return;

            foreach (var dependency in dependencies)
            {
                dependenciesNames.Add(BuildTextField(GetNameText(dependency), "text"));
                dependenciesVersions.Add(BuildTextField(dependency.version, "text"));
                dependenciesStatuses.Add(BuildLabel(GetStatusText(dependency), "text"));

                var dependencyLowWidthItem = new PackageDependencySampleItemLowWidth(GetNameText(dependency), dependency.version.ToString(), BuildLabel(GetStatusText(dependency), "text"));
                dependenciesListLowWidth.Add(dependencyLowWidthItem);
            }
        }

        private void UpdateReverseDependencies(IEnumerable<IPackageVersion> reverseDependencies)
        {
            reverseDependenciesNames.Clear();
            reverseDependenciesVersions.Clear();
            reverseDependenciesListLowWidth.Clear();

            var hasReverseDependencies = reverseDependencies?.Any() ?? false;
            UIUtils.SetElementDisplay(noReverseDependencies, !hasReverseDependencies);
            UIUtils.SetElementDisplay(reverseDependenciesNames, hasReverseDependencies);
            UIUtils.SetElementDisplay(reverseDependenciesVersions, hasReverseDependencies);

            if (!hasReverseDependencies)
                return;

            foreach (var version in reverseDependencies)
            {
                reverseDependenciesNames.Add(BuildTextField(version.displayName ?? string.Empty, "text"));
                reverseDependenciesVersions.Add(BuildTextField(version.version.ToString(), "text"));

                var reverseDependencyLowWidthItem = new PackageDependencySampleItemLowWidth(version.displayName ?? string.Empty, version.version.ToString(), null);
                reverseDependenciesListLowWidth.Add(reverseDependencyLowWidthItem);
            }
        }

        private VisualElementCache cache { get; set; }
        private Toggle dependenciesExpander { get { return cache.Get<Toggle>("dependenciesExpander"); } }
        private VisualElement dependenciesOuterContainer { get { return cache.Get<VisualElement>("dependenciesOuterContainer"); } }
        private VisualElement dependenciesNormalView { get { return cache.Get<VisualElement>("dependenciesNormalView"); } }
        private Label noDependencies { get { return cache.Get<Label>("noDependencies"); } }
        private VisualElement dependenciesNames { get { return cache.Get<VisualElement>("dependenciesNames"); } }
        private VisualElement dependenciesVersions { get { return cache.Get<VisualElement>("dependenciesVersions"); } }
        private VisualElement dependenciesStatuses { get { return cache.Get<VisualElement>("dependenciesStatuses"); } }
        private VisualElement dependenciesListLowWidth { get { return cache.Get<VisualElement>("dependenciesListLowWidth"); } }

        private VisualElement reverseDependenciesNormalView { get { return cache.Get<VisualElement>("reverseDependenciesNormalView"); } }
        private Label noReverseDependencies { get { return cache.Get<Label>("noReverseDependencies"); } }
        private VisualElement reverseDependenciesNames { get { return cache.Get<VisualElement>("reverseDependenciesNames"); } }
        private VisualElement reverseDependenciesVersions { get { return cache.Get<VisualElement>("reverseDependenciesVersions"); } }
        private VisualElement reverseDependenciesListLowWidth { get { return cache.Get<VisualElement>("reverseDependenciesListLowWidth"); } }
    }
}
