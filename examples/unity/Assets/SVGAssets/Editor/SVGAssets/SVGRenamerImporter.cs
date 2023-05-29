/****************************************************************************
** Copyright (c) 2013-2023 Mazatech S.r.l.
** All rights reserved.
** 
** Redistribution and use in source and binary forms, with or without
** modification, are permitted (subject to the limitations in the disclaimer
** below) provided that the following conditions are met:
** 
** - Redistributions of source code must retain the above copyright notice,
**   this list of conditions and the following disclaimer.
** 
** - Redistributions in binary form must reproduce the above copyright notice,
**   this list of conditions and the following disclaimer in the documentation
**   and/or other materials provided with the distribution.
** 
** - Neither the name of Mazatech S.r.l. nor the names of its contributors
**   may be used to endorse or promote products derived from this software
**   without specific prior written permission.
** 
** NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED
** BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
** CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT
** NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
** A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER
** OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
** EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
** PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
** OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
** WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
** OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
** ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
** 
** For any information, please contact info@mazatech.com
** 
****************************************************************************/
using System.IO;
using UnityEditor;

namespace SVGAssets
{
    public class SVGRenamerImporter : AssetPostprocessor
    {
        // move / rename an asset
        private static void AssetMove(string oldAssetPath,
                                      string newAssetPath)
        {
            // remove previous .meta (if exists)
            FileUtil.DeleteFileOrDirectory(oldAssetPath + ".meta");
            // rename font file by appending a .bytes extension
            FileUtil.MoveFileOrDirectory(oldAssetPath, newAssetPath);
            // refresh the database
            AssetDatabase.Refresh();
        }

        private static void SVGResourceImport(SVGResourceType type,
                                              string fileExt,
                                              string assetPath)
        {
            string subject = (type == SVGResourceType.Font) ? "Font" : "Image";
            string unityClass = (type == SVGResourceType.Font) ? "TextMesh" : "Texture2D";

            // because Unity already recognizes font and images files, we must warn the user
            bool ok = EditorUtility.DisplayDialog(string.Format("{0} import for SVGAssets", subject),
                                                  string.Format("Would you like to import {0} {1} for SVGAssets? If no, Unity will import it as a {2} and it won't be usable from SVGAssets", assetPath, subject.ToLower(), unityClass),
                                                  "Import", "Do NOT import");

            if (ok)
            {
                // try to change file extension by appending ".bytes", so Unity can recognize those files as (binary) text assets
                string newAssetPath = Path.ChangeExtension(assetPath, fileExt + ".bytes");

                // does a file with this .bytes extension already exist?
                if (File.Exists(newAssetPath))
                {
                    ok = EditorUtility.DisplayDialog(string.Format("{0} already exists!", subject),
                                                     string.Format("{0} already exists, would you like to overwrite it?", newAssetPath),
                                                     "Import and overwrite", "Do NOT import");

                    if (ok)
                    {
                        // remove the already existing asset file
                        ok = AssetDatabase.DeleteAsset(newAssetPath);
                    }
                }

                if (ok)
                {
                    // move/rename the asset
                    AssetMove(assetPath, newAssetPath);
                }
            }
        }

        // check if the given asset is a font or image that can be used by AmanithSVG as a resource
        private static void SVGResourceImport(string fileExt,
                                              string assetPath)
        {
            switch (fileExt)
            {
                // image files
                case ".jpg":
                case ".jpeg":
                case ".png":
                    SVGResourceImport(SVGResourceType.Image, fileExt, assetPath);
                    break;
                // font files
                case ".otf":
                case ".ttf":
                case ".woff":
                case ".woff2":
                    SVGResourceImport(SVGResourceType.Font, fileExt, assetPath);
                    break;
                default:
                    // nothing to do
                    break;
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets,
                                           string[] deletedAssets,
                                           string[] movedAssets,
                                           string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                // the asset must be located in Assets directory, to prevent importing images
                // and other resources from other locations (as it happens, for example, with
                // Unity for Mac when it downloads packages into the cache directory)
                if (assetPath.StartsWith("Assets"))
                {
                    string fileExt = Path.GetExtension(assetPath).ToLower();

                    // SVG files
                    if (fileExt.Equals(".svg"))
                    {
                        bool ok = true;
                        // try to change ".svg" file extension with ".svg.txt", so Unity can recognize those files as text assets
                        string newAssetPath = Path.ChangeExtension(assetPath, ".svg.txt");

                        // does a file with this .svg.txt extension already exist?
                        if (File.Exists(newAssetPath))
                        {
                            ok = EditorUtility.DisplayDialog("SVG already exists!",
                                                             string.Format("{0} already exists, would you like to import it anyway and overwrite the previous one?", newAssetPath),
                                                             "Import and overwrite", "Do NOT import");
                            if (ok)
                            {
                                // remove the already existing asset file
                                ok = AssetDatabase.DeleteAsset(newAssetPath);
                            }
                        }
                        // do the actual rename
                        if (ok)
                        {
                            // move/rename the asset
                            AssetMove(assetPath, newAssetPath);
                        }
                    }
                    else
                    {
                        // check if the given asset is a font or image that can be used by AmanithSVG as a resource
                        SVGResourceImport(fileExt, assetPath);
                    }
                }
            }
        }
    }
}
