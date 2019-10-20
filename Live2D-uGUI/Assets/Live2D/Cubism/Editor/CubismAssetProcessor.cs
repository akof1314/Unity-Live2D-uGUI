/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Editor.Deleters;
using Live2D.Cubism.Editor.Importers;
using System.IO;
using System.Xml.Linq;
using UnityEditor;


namespace Live2D.Cubism.Editor
{
    /// <summary>
    /// Hooks into Unity's asset pipeline allowing custom processing of assets.
    /// </summary>
    public class CubismAssetProcessor : AssetPostprocessor
    {
        #region Unity Event Handling

        /// <summary>
        /// Called by Unity. Makes sure <see langword="unsafe"/> code is allowed.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static void OnGeneratedCSProjectFiles()
        {
            AllowUnsafeCode();
        }


        /// <summary>
        /// Called by Unity on asset import. Handles importing of Cubism related assets.
        /// </summary>
        /// <param name="importedAssetPaths">Paths of imported assets.</param>
        /// <param name="deletedAssetPaths">Paths of removed assets.</param>
        /// <param name="movedAssetPaths">Paths of moved assets</param>
        /// <param name="movedFromAssetPaths">Paths of moved assets before moving</param>
        private static void OnPostprocessAllAssets(
            string[] importedAssetPaths,
            string[] deletedAssetPaths,
            string[] movedAssetPaths,
            string[] movedFromAssetPaths)
        {
            // Handle any imported Cubism assets.
            foreach (var assetPath in importedAssetPaths)
            {
                var importer = CubismImporter.GetImporterAtPath(assetPath);


                if (importer == null)
                {
                    continue;
                }


                importer.Import();
            }


            // Handle any deleted Cubism assets.
            foreach (var assetPath in deletedAssetPaths)
            {
                var deleter = CubismDeleter.GetDeleterAsPath(assetPath);

                if (deleter == null)
                {
                    continue;
                }

                deleter.Delete();
            }

        }

        #endregion

        #region C# Project Patching

        /// <summary>
        /// Makes sure <see langword="unsafe"/> code is allowed in the runtime project.
        /// </summary>
        private static void AllowUnsafeCode()
        {
            foreach (var csproj in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj"))
            {
                // Skip Editor assembly.
                if (csproj.EndsWith(".Editor.csproj"))
                {
                    continue;
                }


                var document = XDocument.Load(csproj);
                var project = document.Root;


                // Allow unsafe code.
                for (var propertyGroup = project.FirstNode as XElement; propertyGroup != null; propertyGroup = propertyGroup.NextNode as XElement)
                {
                    // Skip non-relevant groups.
                    if (!propertyGroup.ToString().Contains("PropertyGroup") || !propertyGroup.ToString().Contains("$(Configuration)|$(Platform)"))
                    {
                        continue;
                    }


                    // Add unsafe-block element if necessary.
                    if (!propertyGroup.ToString().Contains("AllowUnsafeBlocks"))
                    {
                        var nameSpace = propertyGroup.GetDefaultNamespace();


                        propertyGroup.Add(new XElement(nameSpace + "AllowUnsafeBlocks", "true"));
                    }


                    // Make sure unsafe-block element is always set to true.
                    for (var allowUnsafeBlocks = propertyGroup.FirstNode as XElement; allowUnsafeBlocks != null; allowUnsafeBlocks = allowUnsafeBlocks.NextNode as XElement)
                    {
                        if (!allowUnsafeBlocks.ToString().Contains("AllowUnsafeBlocks"))
                        {
                            continue;
                        }


                        allowUnsafeBlocks.SetValue("true");
                    }
                }


                // Store changes.
                document.Save(csproj);
            }
        }

        #endregion
    }
}
