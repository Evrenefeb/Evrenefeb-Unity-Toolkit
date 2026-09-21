using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace Evrenefeb.Toolkit.Editor {
    public static class ToolbarModuleController {


        private const string PackageName = "com.evrenefeb.unity-toolkit";

        // key: sample displayName, value: bu sample'in çalışması için önce kurulması gereken sample'lar
    //    private static readonly Dictionary<string, string[]> Dependencies = new()
    //    {
    //    { "Sample D", new string[0] },
    //    { "Sample A", new[] { "Sample B" } },
    //    { "Sample B", new string[0] },
    //    { "Sample E", new string[0] },
    //    { "Sample C", new[] { "Sample E", "Sample B" } },
    //};

        private static readonly Dictionary<string, string[]> Dependencies = new()
        {
            { "Persistence", new string[0] },
        };

        private static Dictionary<string, List<string>> _dependents;
        private static Dictionary<string, List<string>> Dependents {
            get {
                if (_dependents != null) return _dependents;

                _dependents = Dependencies.Keys.ToDictionary(k => k, k => new List<string>());
                foreach (var kvp in Dependencies)
                    foreach (var dep in kvp.Value)
                        if (_dependents.TryGetValue(dep, out var list))
                            list.Add(kvp.Key);

                return _dependents;
            }
        }

        // ------------------------------------------------------------
        // Import komutları
        // ------------------------------------------------------------

        [MenuItem("Tools/Evrenefeb Toolkit/Import/Persistence", false, 10)]
        private static void ImportPersistence() => ImportWithDependencies("Persistence");
        [MenuItem("Tools/Evrenefeb Toolkit/Import/Persistence", true)]
        private static bool ValidateImportPersistence() => !IsImported("Persistence");

        #region Templetes

        //[MenuItem("MyToolkit/Import/Sample A", false, 10)]
        //private static void ImportA() => ImportWithDependencies("Sample A");
        //[MenuItem("MyToolkit/Import/Sample A", true)]
        //private static bool ValidateImportA() => !IsImported("Sample A");

        //[MenuItem("MyToolkit/Import/Sample B", false, 11)]
        //private static void ImportB() => ImportWithDependencies("Sample B");
        //[MenuItem("MyToolkit/Import/Sample B", true)]
        //private static bool ValidateImportB() => !IsImported("Sample B");

        //[MenuItem("MyToolkit/Import/Sample C", false, 12)]
        //private static void ImportC() => ImportWithDependencies("Sample C");
        //[MenuItem("MyToolkit/Import/Sample C", true)]
        //private static bool ValidateImportC() => !IsImported("Sample C");

        //[MenuItem("MyToolkit/Import/Sample D", false, 13)]
        //private static void ImportD() => ImportWithDependencies("Sample D");
        //[MenuItem("MyToolkit/Import/Sample D", true)]
        //private static bool ValidateImportD() => !IsImported("Sample D");

        //[MenuItem("MyToolkit/Import/Sample E", false, 14)]
        //private static void ImportE() => ImportWithDependencies("Sample E");
        //[MenuItem("MyToolkit/Import/Sample E", true)]
        //private static bool ValidateImportE() => !IsImported("Sample E");

        //[MenuItem("MyToolkit/Import/All Samples", false, 30)]
        //private static void ImportAll() {
        //    foreach (var name in Dependencies.Keys)
        //        ImportWithDependencies(name);
        //}

        #endregion


        // ------------------------------------------------------------
        // Remove / Uninstall komutları
        // ------------------------------------------------------------

        [MenuItem("Tools/Evrenefeb Toolkit/Remove/Persistence", false, 60)]
        private static void RemovePersistence() => RemoveWithDependents("Persistence");
        [MenuItem("Tools/Evrenefeb Toolkit/Remove/Persistence", true)]
        private static bool ValidateRemovePersistence() => IsImported("Persistence");


        // ------------------------------------------------------------
        // Status göstergesi
        // ------------------------------------------------------------

        [MenuItem("Tools/Evrenefeb Toolkit/Status/Persistence", false, 100)]
        private static void StatusPersistence() { }
        [MenuItem("Tools/Evrenefeb Toolkit/Status/Persistence", true)]
        private static bool ValidateStatusPersistence() { Menu.SetChecked("Tools/Evrenefeb Toolkit/Import/Persistence", IsImported("Persistence")); return false; }

        
        // ------------------------------------------------------------
        // Ortak yardımcı metotlar
        // ------------------------------------------------------------

        private static Dictionary<string, Sample> GetSamples() {
            return Sample.FindByPackage(PackageName, null).ToDictionary(s => s.displayName);
        }

        private static bool IsImported(string sampleName) {
            var samples = GetSamples();
            return samples.TryGetValue(sampleName, out var sample) && sample.isImported;
        }

        private static void ImportWithDependencies(string sampleName) {
            var samples = GetSamples();
            var imported = new List<string>();

            void ImportRecursive(string name, HashSet<string> visited) {
                if (!visited.Add(name)) return; // döngüsel bağımlılığa karşı koruma

                if (Dependencies.TryGetValue(name, out var deps))
                    foreach (var dep in deps)
                        ImportRecursive(dep, visited);

                if (samples.TryGetValue(name, out var sample) && !sample.isImported) {
                    sample.Import();
                    imported.Add(name);
                }
            }


            ImportRecursive(sampleName, new HashSet<string>());

            if (imported.Count > 0)
                Debug.Log($"[MyToolkit] İçe aktarıldı: {string.Join(", ", imported)}");
            else
                Debug.Log($"[MyToolkit] '{sampleName}' zaten kurulu (bağımlılıklarıyla birlikte).");
        }

        /// <summary>
        /// Bir sample'ı kaldırır. Eğer başka kurulu sample'lar buna bağımlıysa
        /// (dependents), önce kullanıcıya sorar; onaylanırsa onları da kaskad
        /// olarak kaldırır. Onaylanmazsa hiçbir şey silinmez.
        /// </summary>
        private static void RemoveWithDependents(string sampleName) {
            var toRemove = new List<string>();
            CollectDependentsRecursive(sampleName, toRemove, new HashSet<string>());

            // sampleName'in kendisi de listeye dahil
            if (!toRemove.Contains(sampleName))
                toRemove.Add(sampleName);

            // Sadece gerçekten kurulu olanları göster/kaldır
            toRemove = toRemove.Where(IsImported).ToList();

            if (toRemove.Count > 1) {
                var others = toRemove.Where(n => n != sampleName);
                bool confirmed = EditorUtility.DisplayDialog(
                    "Bağımlı Sample'lar Bulundu",
                    $"'{sampleName}' kaldırılırsa şunlar da bozulacağı için birlikte kaldırılacak:\n\n" +
                    $"{string.Join("\n", others)}\n\nDevam edilsin mi?",
                    "Evet, Hepsini Kaldır", "İptal");

                if (!confirmed) {
                    Debug.Log($"[MyToolkit] Kaldırma işlemi iptal edildi: '{sampleName}'.");
                    return;
                }
            }

            foreach (var name in toRemove)
                RemoveInternal(name);

            AssetDatabase.Refresh();
            Debug.Log($"[MyToolkit] Kaldırıldı: {string.Join(", ", toRemove)}");
        }

        /// <summary>
        /// sampleName'e (doğrudan veya dolaylı olarak) bağımlı olan,
        /// kurulu tüm sample'ları bulur.
        /// </summary>
        private static void CollectDependentsRecursive(string sampleName, List<string> result, HashSet<string> visited) {
            if (!visited.Add(sampleName)) return;

            if (Dependents.TryGetValue(sampleName, out var directDependents)) {
                foreach (var dep in directDependents) {
                    if (IsImported(dep)) {
                        result.Add(dep);
                        CollectDependentsRecursive(dep, result, visited);
                    }
                }
            }
        }

        /// <summary>
        /// Onay/kaskad mantığı olmadan, doğrudan diskten siler.
        /// Sample.importPath, sample'ın Assets altındaki kopyalandığı yerdir.
        /// </summary>
        private static void RemoveInternal(string sampleName) {
            var samples = GetSamples();
            if (!samples.TryGetValue(sampleName, out var sample) || !sample.isImported)
                return;

            string path = sample.importPath;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) {
                Debug.LogWarning($"[MyToolkit] '{sampleName}' için import yolu bulunamadı: {path}");
                return;
            }

            try {
                // AssetDatabase üzerinden silmek .meta dosyalarını da temizler
                if (!AssetDatabase.DeleteAsset(ToAssetsRelativePath(path))) {
                    // Proje dışı/relatif olmayan bir yol ise fallback: doğrudan dosya sistemi
                    Directory.Delete(path, true);
                    string metaFile = path.TrimEnd('/', '\\') + ".meta";
                    if (File.Exists(metaFile))
                        File.Delete(metaFile);
                }
            }
            catch (Exception e) {
                Debug.LogError($"[MyToolkit] '{sampleName}' kaldırılırken hata oluştu: {e.Message}");
            }
        }

        private static string ToAssetsRelativePath(string fullPath) {
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string full = Path.GetFullPath(fullPath);
            if (full.StartsWith(projectPath))
                return full.Substring(projectPath.Length + 1).Replace('\\', '/');
            return fullPath;
        }
    }
}
