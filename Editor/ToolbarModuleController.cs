using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
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
            { "Improved Timers Module", new string[0] },
            { "Input Module", new string[0] },
            { "GameManagement Module", new string[0] },
        };

        // key: sample displayName, value: (harici paket adı, git URL) çiftleri.
        // Paket adını her kütüphanenin kendi package.json'undaki "name" alanından al.
        private static readonly Dictionary<string, (string packageName, string gitUrl)[]> ExternalDependencies = new()
        {
            {
                "Improved Timers Module",
                new[] {
                    ("com.gitamend.improvedtimers", "https://github.com/adammyhre/Unity-Improved-Timers.git")
                }
            }
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

        #region Import / Remove / Status

        // ------------------------------------------------------------
        // Import / Remove / Status komutları
        // ------------------------------------------------------------

        #region Import

        // ------------------------------------------------------------
        // Import komutları
        // ------------------------------------------------------------

        [MenuItem("Tools/Evrenefeb Toolkit/Import/Persistence Module", false, 10)]
        private static void ImportPersistence() => Import("Persistence Module");
        [MenuItem("Tools/Evrenefeb Toolkit/Import/Persistence Module", true)]
        private static bool ValidateImportPersistence() => !IsImported("Persistence Module");



        [MenuItem("Tools/Evrenefeb Toolkit/Import/Improved Timers Module", false, 11)]
        private static void ImportTimersModule() => Import("Improved Timers Module");
        [MenuItem("Tools/Evrenefeb Toolkit/Import/Improved Timers Module", true)]
        private static bool ValidateImportTimersModule() => !IsImported("Improved Timers Module");



        [MenuItem("Tools/Evrenefeb Toolkit/Import/Input Module", false, 11)]
        private static void ImportInputModule() => Import("Input Module");
        [MenuItem("Tools/Evrenefeb Toolkit/Import/Input Module", true)]
        private static bool ValidateImportInputModule() => !IsImported("Input Module");



        [MenuItem("Tools/Evrenefeb Toolkit/Import/GameManagement Module", false, 11)]
        private static void ImportGameManagementModule() => Import("GameManagement Module");
        [MenuItem("Tools/Evrenefeb Toolkit/Import/GameManagement Module", true)]
        private static bool ValidateImportGameManagementModule() => !IsImported("GameManagement Module");

        #endregion

        #region Remove

        // ------------------------------------------------------------
        // Remove / Uninstall komutları
        // ------------------------------------------------------------

        [MenuItem("Tools/Evrenefeb Toolkit/Remove/Persistence Module", false, 60)]
        private static void RemovePersistence() => RemoveWithDependents("Persistence Module");
        [MenuItem("Tools/Evrenefeb Toolkit/Remove/Persistence Module", true)]
        private static bool ValidateRemovePersistence() => IsImported("Persistence Module");


        [MenuItem("Tools/Evrenefeb Toolkit/Remove/Improved Timers Module", false, 61)]
        private static void RemoveTimersModule() => RemoveWithDependents("Improved Timers Module");
        [MenuItem("Tools/Evrenefeb Toolkit/Remove/Improved Timers Module", true)]
        private static bool ValidateRemoveTimersModule() => IsImported("Improved Timers Module");



        [MenuItem("Tools/Evrenefeb Toolkit/Remove/Input Module", false, 61)]
        private static void RemoveInputModule() => RemoveWithDependents("Input Module");
        [MenuItem("Tools/Evrenefeb Toolkit/Remove/Input Module", true)]
        private static bool ValidateRemoveInputModule() => IsImported("Input Module");



        [MenuItem("Tools/Evrenefeb Toolkit/Remove/GameManagement Module", false, 61)]
        private static void RemoveGameManagementModule() => RemoveWithDependents("GameManagement Module");
        [MenuItem("Tools/Evrenefeb Toolkit/Remove/GameManagement Module", true)]
        private static bool ValidateRemoveGameManagementModule() => IsImported("GameManagement Module");

        #endregion

        #region Status

        // ------------------------------------------------------------
        // Status göstergesi
        // ------------------------------------------------------------

        [MenuItem("Tools/Evrenefeb Toolkit/Status/Persistence Module", false, 100)]
        private static void StatusPersistence() { }
        [MenuItem("Tools/Evrenefeb Toolkit/Status/Persistence Module", true)]
        private static bool ValidateStatusPersistence() { Menu.SetChecked("Tools/Evrenefeb Toolkit/Status/Persistence Module", IsImported("Persistence Module")); return false; }


        [MenuItem("Tools/Evrenefeb Toolkit/Status/Improved Timers Module", false, 101)]
        private static void StatusTimersModule() { }
        [MenuItem("Tools/Evrenefeb Toolkit/Status/Improved Timers Module", true)]
        private static bool ValidateStatusTimersModule() { Menu.SetChecked("Tools/Evrenefeb Toolkit/Status/Improved Timers Module", IsImported("Improved Timers Module")); return false; }



        [MenuItem("Tools/Evrenefeb Toolkit/Status/Input Module", false, 101)]
        private static void StatusInputModule() { }
        [MenuItem("Tools/Evrenefeb Toolkit/Status/Input Module", true)]
        private static bool ValidateStatusInputModule() { Menu.SetChecked("Tools/Evrenefeb Toolkit/Status/Input Module", IsImported("GameManagement Module")); return false; }



        [MenuItem("Tools/Evrenefeb Toolkit/Status/GameManagement Module", false, 101)]
        private static void StatusGameManagementModule() { }
        [MenuItem("Tools/Evrenefeb Toolkit/Status/GameManagement Module", true)]
        private static bool ValidateStatusGameManagementModule() { Menu.SetChecked("Tools/Evrenefeb Toolkit/Status/GameManagement Module", IsImported("GameManagement Module")); return false; }

        #endregion

        #endregion

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

        /// <summary>
        /// Bir sample'ı import etmeden önce, o sample'ın ihtiyaç duyduğu
        /// harici (third-party) UPM paketlerinin kurulu olup olmadığını kontrol eder.
        /// Eksik olanlar varsa kullanıcıdan onay alıp git URL üzerinden otomatik kurar,
        /// kurulum bitince asıl sample import zincirini (ImportWithDependencies) tetikler.
        /// Harici bağımlılığı olmayan sample'lar (örn. Persistence) doğrudan geçer.
        /// </summary>
        private static void Import(string sampleName) {
            var missingExternals = GetMissingExternalPackages(sampleName);

            if (missingExternals.Count == 0) {
                ImportWithDependencies(sampleName);
                return;
            }

            var names = string.Join("\n", missingExternals.Select(m => $"• {m.packageName}"));
            bool confirmed = EditorUtility.DisplayDialog(
                "Harici Paket Gerekiyor",
                $"'{sampleName}' için şu harici paketlerin kurulması gerekiyor:\n\n{names}\n\n" +
                "Bunlar Git URL üzerinden otomatik kurulacak. Devam edilsin mi?",
                "Evet, Kur ve Import Et", "İptal");

            if (!confirmed) {
                Debug.Log($"[MyToolkit] '{sampleName}' import işlemi iptal edildi (harici paket onayı verilmedi).");
                return;
            }

            var gitUrls = missingExternals.Select(m => m.gitUrl).ToArray();

            Debug.Log($"[MyToolkit] Harici paketler kuruluyor: {string.Join(", ", gitUrls)}");
            var request = Client.AddAndRemove(gitUrls, null);

            void Poll() {
                if (!request.IsCompleted) return;

                EditorApplication.update -= Poll;

                if (request.Status == StatusCode.Success) {
                    Debug.Log($"[MyToolkit] Harici paketler kuruldu, '{sampleName}' import ediliyor...");
                    // Package Manager yeniden çözümleme yaptığı için bir frame beklemek
                    // Sample.FindByPackage sonuçlarının güncel olmasını garantiler.
                    EditorApplication.delayCall += () => ImportWithDependencies(sampleName);
                }
                else {
                    Debug.LogError($"[MyToolkit] Harici paket kurulumu başarısız oldu: {request.Error?.message}");
                }
            }

            EditorApplication.update += Poll;
        }

        private static List<(string packageName, string gitUrl)> GetMissingExternalPackages(string sampleName) {
            var result = new List<(string, string)>();
            if (!ExternalDependencies.TryGetValue(sampleName, out var externals))
                return result;

            var installed = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .Select(p => p.name)
                .ToHashSet();

            foreach (var ext in externals)
                if (!installed.Contains(ext.packageName))
                    result.Add(ext);

            return result;
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
        /// Ayrıca, kaldırılan sample'ların ihtiyaç duyduğu harici (third-party)
        /// UPM paketlerini de kontrol eder: eğer o paket, kaldırma sonrasında
        /// hâlâ kurulu kalan başka bir sample tarafından kullanılmıyorsa,
        /// Package Manager'dan da otomatik olarak sökülür.
        /// </summary>
        private static void RemoveWithDependents(string sampleName) {
            var toRemove = new List<string>();
            CollectDependentsRecursive(sampleName, toRemove, new HashSet<string>());

            // sampleName'in kendisi de listeye dahil
            if (!toRemove.Contains(sampleName))
                toRemove.Add(sampleName);

            // Sadece gerçekten kurulu olanları göster/kaldır
            toRemove = toRemove.Where(IsImported).ToList();

            // Kaldırılacak sample'ların ihtiyaç duyduğu harici paketler (aday liste)
            var candidateExternals = GetRequiredExternalPackages(toRemove);

            // Kaldırma sonrasında hâlâ kurulu kalacak sample'ların ihtiyaç duyduğu paketler
            var stillImportedAfter = Dependencies.Keys.Where(IsImported).Except(toRemove);
            var stillNeededExternals = GetRequiredExternalPackages(stillImportedAfter);

            // Artık kimse tarafından kullanılmayan harici paketler
            var externalsToRemove = candidateExternals.Except(stillNeededExternals).ToList();

            string message = $"'{sampleName}' kaldırılacak.";
            var others = toRemove.Where(n => n != sampleName).ToList();
            if (others.Count > 0)
                message += $"\n\nBuna bağımlı olduğu için şunlar da birlikte kaldırılacak:\n{string.Join("\n", others)}";

            if (externalsToRemove.Count > 0)
                message += $"\n\nAyrıca artık hiçbir sample tarafından kullanılmayan şu harici paketler de Package Manager'dan kaldırılacak:\n{string.Join("\n", externalsToRemove)}";

            if (others.Count > 0 || externalsToRemove.Count > 0) {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Kaldırma Onayı",
                    message + "\n\nDevam edilsin mi?",
                    "Evet, Kaldır", "İptal");

                if (!confirmed) {
                    Debug.Log($"[MyToolkit] Kaldırma işlemi iptal edildi: '{sampleName}'.");
                    return;
                }
            }

            foreach (var name in toRemove)
                RemoveInternal(name);

            AssetDatabase.Refresh();
            Debug.Log($"[MyToolkit] Kaldırıldı: {string.Join(", ", toRemove)}");

            if (externalsToRemove.Count > 0)
                RemoveExternalPackages(externalsToRemove);
        }

        /// <summary>
        /// Verilen sample isimleri için ExternalDependencies haritasından
        /// gereken tüm harici paket adlarının kümesini döner.
        /// </summary>
        private static HashSet<string> GetRequiredExternalPackages(IEnumerable<string> sampleNames) {
            var result = new HashSet<string>();
            foreach (var name in sampleNames)
                if (ExternalDependencies.TryGetValue(name, out var externals))
                    foreach (var ext in externals)
                        result.Add(ext.packageName);
            return result;
        }

        /// <summary>
        /// Verilen harici paketleri Unity Package Manager'dan tamamen söker
        /// (manifest.json'dan kaldırır). Asenkron olduğu için tamamlanana
        /// kadar EditorApplication.update ile beklenir.
        /// </summary>
        private static void RemoveExternalPackages(List<string> packageNames) {
            Debug.Log($"[MyToolkit] Harici paketler Package Manager'dan kaldırılıyor: {string.Join(", ", packageNames)}");
            var request = Client.AddAndRemove(null, packageNames.ToArray());

            void Poll() {
                if (!request.IsCompleted) return;

                EditorApplication.update -= Poll;

                if (request.Status == StatusCode.Success)
                    Debug.Log($"[MyToolkit] Harici paketler başarıyla kaldırıldı: {string.Join(", ", packageNames)}");
                else
                    Debug.LogError($"[MyToolkit] Harici paketler kaldırılırken hata oluştu: {request.Error?.message}");
            }

            EditorApplication.update += Poll;
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