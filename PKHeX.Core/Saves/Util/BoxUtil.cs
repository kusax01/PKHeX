using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using static PKHeX.Core.MessageStrings;

namespace PKHeX.Core;

/// <summary>
/// Contains extension methods for use with a <see cref="SaveFile"/>.
/// </summary>
public static class BoxUtil
{
    /// <summary>
    /// Dumps a folder of files to the <see cref="SaveFile"/>.
    /// </summary>
    /// <param name="sav"><see cref="SaveFile"/> that is being dumped from.</param>
    /// <param name="path">Folder to store <see cref="PKM"/> files.</param>
    /// <param name="boxFolders">Option to save in child folders with the Box Name as the folder name.</param>
    /// <returns>-1 if aborted, otherwise the amount of files dumped.</returns>
    public static int DumpBoxes(this SaveFile sav, string path, bool boxFolders = false)
    {
        //KuSax - Header of pokemon_list CSV file - Start of the file 
        string pokemon_list = "";
        pokemon_list += $"National dex,Form, Regional dex,French name,English name, Poke, Beast, Dream, Outside Raids\n";

        //KuSax - Header of filter.json filters for raidcrawler
        string filter = "[";
        //KuSax - Add manual pk filters to filter.json file for raidcrawler
        filter += "{\"Name\":\"0atk 0spe ditto\",\"Species\":132,\"Form\":0,\"Stars\":null,\"StarsComp\":0,\"Shiny\":false,\"Nature\":null,\"TeraType\":null,\"Gender\":null,\"IVBin\":63,\"IVComps\":0,\"IVVals\":33553439,\"Enabled\":true,\"RewardItems\":null,\"RewardsComp\":-1,\"RewardsCount\":0,\"BatchFilters\":null},";
        //KuSax - always use comma , as separator for pk filter at the end of file
        filter += "{\"Name\":\"Herba\",\"Species\":null,\"Stars\":null,\"StarsComp\":0,\"Shiny\":false,\"Nature\":null,\"TeraType\":null,\"Gender\":null,\"IVBin\":0,\"IVComps\":0,\"IVVals\":1073741823,\"Enabled\":true,\"RewardItems\":[1904,1905,1906,1907,1908],\"RewardsComp\":2,\"RewardsCount\":8,\"BatchFilters\":null},";



        if (!sav.HasBox)
            return -1;

        var boxData = sav.BoxData;
        int boxSlotCount = sav.BoxSlotCount;
        var ctr = 0;
        for (var slot = 0; slot < boxData.Count; slot++)
        {
            var pk = boxData[slot];
            var box = slot / boxSlotCount;
            if (pk.Species == 0 || !pk.Valid)
                continue;

            var boxFolder = path;
            if (boxFolders)
            {
                var boxName = Util.CleanFileName(sav.GetBoxName(box));
                boxFolder = Path.Combine(path, boxName);
                Directory.CreateDirectory(boxFolder);
            }

            var fileName = Util.CleanFileName(pk.FileName);
            var fn = Path.Combine(boxFolder, fileName);
            if (File.Exists(fn))
                continue;

            File.WriteAllBytes(fn, pk.DecryptedPartyData);
            ctr++;

            var path_poke = "D:\\Documents\\GitHub\\PokemonDatabase\\SV\\living dex shiny\\poke\\";
            var files_poke = Directory.EnumerateFiles(path_poke, $"{pk.Species:0000}-{pk.Form:00}*", SearchOption.AllDirectories);

            var path_beast = "D:\\Documents\\GitHub\\PokemonDatabase\\SV\\living dex shiny\\beast\\";
            var files_beast = Directory.EnumerateFiles(path_beast, $"{pk.Species:0000}-{pk.Form:00}*", SearchOption.AllDirectories);

            var path_dream = "D:\\Documents\\GitHub\\PokemonDatabase\\SV\\living dex shiny\\dream\\";
            var files_dream = Directory.EnumerateFiles(path_dream, $"{pk.Species:0000}-{pk.Form:00}*", SearchOption.AllDirectories);

            var path_outside_raids = "D:\\Documents\\GitHub\\PokemonDatabase\\SV\\living dex shiny\\outside raids\\";
            var files_outside_raids = Directory.EnumerateFiles(path_outside_raids, $"{pk.Species:0000}-{pk.Form:00}*", SearchOption.AllDirectories);

            var spanish_name = SpeciesName.GetSpeciesNameGeneration(pk.Species, (int)LanguageID.Spanish, pk.Format);
            var english_name = SpeciesName.GetSpeciesNameGeneration(pk.Species, (int)LanguageID.English, pk.Format);

            //KuSax - trying to add pokedex regional number to raidcrawler json filters
            //var pokedex_regionalnmb = SpeciesName.GetSpeciesNameGeneration(pk.Gen9 ;//, (int)LanguageID.English, pk.Format);

            //KuSax - add all pokemon to the csv file, marking true/false if exist on pokemon directories
            pokemon_list += $"{pk.Species:0000},{pk.Form:00},{((PersonalInfo9SV)pk.PersonalInfo).DexIndex},{english_name},{spanish_name},{files_poke.Any()},{files_beast.Any()},{files_dream.Any()},{files_outside_raids.Count()}\n";

            //KuSax - add pokemon not encountered on the directories to the filters.json file
            if (!files_beast.Any() &&
                !files_dream.Any() &&
                !files_outside_raids.Any() &&
                !files_poke.Any())
            {
                filter += $"{{\"Name\":\"{english_name} {spanish_name} shiny {((PersonalInfo9SV)pk.PersonalInfo).DexIndex}\",\"Species\":{pk.Species},\"Form\":{pk.Form},\"Stars\":null,\"StarsComp\":0,\"Shiny\":true,\"Nature\":null,\"TeraType\":null,\"Gender\":null,\"IVBin\":0,\"IVComps\":0,\"IVVals\":33553439,\"Enabled\":true,\"RewardItems\":null,\"RewardsComp\":-1,\"RewardsCount\":0,\"BatchFilters\":null}},";
            }
            if (files_beast.Count() > 1 ||
                files_dream.Count() > 1 ||
                files_poke.Count() > 1)
            {
                Debugger.Break();
            }
        }

        //KuSax - finish the csv file
        File.WriteAllText("pokemon_list.csv", pokemon_list);

        //KuSax - finish the filter.json file
        filter += "]";
        File.WriteAllText("filters.json", filter);

        return ctr;
    }

    /// <summary>
    /// Dumps the <see cref="SaveFile.BoxData"/> to a folder with individual decrypted files.
    /// </summary>
    /// <param name="sav"><see cref="SaveFile"/> that is being dumped from.</param>
    /// <param name="path">Folder to store <see cref="PKM"/> files.</param>
    /// <param name="currentBox">Box contents to be dumped.</param>
    /// <returns>-1 if aborted, otherwise the amount of files dumped.</returns>
    public static int DumpBox(this SaveFile sav, string path, int currentBox)
    {
        if (!sav.HasBox)
            return -1;

        var boxData = sav.BoxData;
        int boxSlotCount = sav.BoxSlotCount;
        var ctr = 0;
        for (var slot = 0; slot < boxData.Count; slot++)
        {
            var pk = boxData[slot];
            var box = slot / boxSlotCount;
            if (pk.Species == 0 || !pk.Valid || box != currentBox)
                continue;

            var fileName = Path.Combine(path, Util.CleanFileName(pk.FileName));
            if (File.Exists(fileName))
                continue;

            File.WriteAllBytes(fileName, pk.DecryptedPartyData);
            ctr++;
        }
        return ctr;
    }

    /// <summary>
    /// Loads a folder of files to the <see cref="SaveFile"/>.
    /// </summary>
    /// <param name="sav"><see cref="SaveFile"/> to load folder to.</param>
    /// <param name="path">Folder to load <see cref="PKM"/> files from. Files are only loaded from the top directory.</param>
    /// <param name="result">Result message from the method.</param>
    /// <param name="boxStart">First box to start loading to. All prior boxes are not modified.</param>
    /// <param name="boxClear">Instruction to clear boxes after the starting box.</param>
    /// <param name="overwrite">Overwrite existing full slots. If true, will only overwrite empty slots.</param>
    /// <param name="noSetb">Bypass option to not modify <see cref="PKM"/> properties when setting to Save File.</param>
    /// <param name="all">Enumerate all files even in sub-folders.</param>
    /// <returns>Count of files imported.</returns>
    public static int LoadBoxes(this SaveFile sav, string path, out string result, int boxStart = 0, bool boxClear = false, bool overwrite = false, PKMImportSetting noSetb = PKMImportSetting.UseDefault, bool all = false)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        { result = MsgSaveBoxExportPathInvalid; return -1; }

        var option = all ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.EnumerateFiles(path, "*.*", option);
        return sav.LoadBoxes(files, out result, boxStart, boxClear, overwrite, noSetb);
    }

    /// <summary>
    /// Loads a folder of files to the <see cref="SaveFile"/>.
    /// </summary>
    /// <param name="sav"><see cref="SaveFile"/> to load folder to.</param>
    /// <param name="files">Files to load <see cref="PKM"/> files from.</param>
    /// <param name="result">Result message from the method.</param>
    /// <param name="boxStart">First box to start loading to. All prior boxes are not modified.</param>
    /// <param name="boxClear">Instruction to clear boxes after the starting box.</param>
    /// <param name="overwrite">Overwrite existing full slots. If true, will only overwrite empty slots.</param>
    /// <param name="noSetb">Bypass option to not modify <see cref="PKM"/> properties when setting to Save File.</param>
    /// <returns>Count of files imported.</returns>
    public static int LoadBoxes(this SaveFile sav, IEnumerable<string> files, out string result, int boxStart = 0, bool boxClear = false, bool overwrite = false, PKMImportSetting noSetb = PKMImportSetting.UseDefault)
    {
        var pks = GetPossiblePKMsFromPaths(sav, files);
        return sav.LoadBoxes(pks, out result, boxStart, boxClear, overwrite, noSetb);
    }

    /// <summary>
    /// Loads a folder of files to the <see cref="SaveFile"/>.
    /// </summary>
    /// <param name="sav"><see cref="SaveFile"/> to load folder to.</param>
    /// <param name="encounters">Encounters to create <see cref="PKM"/> files from.</param>
    /// <param name="result">Result message from the method.</param>
    /// <param name="boxStart">First box to start loading to. All prior boxes are not modified.</param>
    /// <param name="boxClear">Instruction to clear boxes after the starting box.</param>
    /// <param name="overwrite">Overwrite existing full slots. If true, will only overwrite empty slots.</param>
    /// <param name="noSetb">Bypass option to not modify <see cref="PKM"/> properties when setting to Save File.</param>
    /// <returns>Count of files imported.</returns>
    public static int LoadBoxes(this SaveFile sav, IEnumerable<IEncounterConvertible> encounters, out string result, int boxStart = 0, bool boxClear = false, bool overwrite = false, PKMImportSetting noSetb = PKMImportSetting.UseDefault)
    {
        var pks = encounters.Select(z => z.ConvertToPKM(sav));
        return sav.LoadBoxes(pks, out result, boxStart, boxClear, overwrite, noSetb);
    }

    /// <summary>
    /// Loads a folder of files to the <see cref="SaveFile"/>.
    /// </summary>
    /// <param name="sav"><see cref="SaveFile"/> to load folder to.</param>
    /// <param name="pks">Unconverted <see cref="PKM"/> objects to load.</param>
    /// <param name="result">Result message from the method.</param>
    /// <param name="boxStart">First box to start loading to. All prior boxes are not modified.</param>
    /// <param name="boxClear">Instruction to clear boxes after the starting box.</param>
    /// <param name="overwrite">Overwrite existing full slots. If true, will only overwrite empty slots.</param>
    /// <param name="noSetb">Bypass option to not modify <see cref="PKM"/> properties when setting to Save File.</param>
    /// <returns>True if any files are imported.</returns>
    public static int LoadBoxes(this SaveFile sav, IEnumerable<PKM> pks, out string result, int boxStart = 0, bool boxClear = false, bool overwrite = false, PKMImportSetting noSetb = PKMImportSetting.UseDefault)
    {
        if (!sav.HasBox)
        { result = MsgSaveBoxFailNone; return -1; }

        var compat = sav.GetCompatible(pks);
        if (boxClear)
            sav.ClearBoxes(boxStart);

        int ctr = sav.ImportPKMs(compat, overwrite, boxStart, noSetb);
        if (ctr <= 0)
        {
            result = MsgSaveBoxImportNoFiles;
            return -1;
        }

        result = string.Format(MsgSaveBoxImportSuccess, ctr);
        return ctr;
    }

    public static IEnumerable<PKM> GetPKMsFromPaths(IEnumerable<string> files, EntityContext generation)
    {
        foreach (var f in files)
        {
            var fi = new FileInfo(f);
            if (!fi.Exists)
                continue;
            if (!EntityDetection.IsSizePlausible(fi.Length))
                continue;
            var data = File.ReadAllBytes(f);
            var prefer = EntityFileExtension.GetContextFromExtension(fi.Extension, generation);
            var pk = EntityFormat.GetFromBytes(data, prefer);
            if (pk?.Species is > 0)
                yield return pk;
        }
    }

    private static IEnumerable<PKM> GetPossiblePKMsFromPaths(SaveFile sav, IEnumerable<string> files)
    {
        foreach (var f in files)
        {
            var obj = FileUtil.GetSupportedFile(f, sav);
            switch (obj)
            {
                case PKM pk:
                    yield return pk;
                    break;
                case MysteryGift {IsEntity: true} g:
                    yield return g.ConvertToPKM(sav);
                    break;
                case IEncounterInfo g when g.Species != 0:
                    yield return g.ConvertToPKM(sav);
                    break;
                case IPokeGroup g:
                    foreach (var p in g.Contents)
                        yield return p;
                    break;
                case IEnumerable<PKM> contents:
                    foreach (var pk in contents)
                        yield return pk;
                    break;
            }
        }
    }

    /// <summary>
    /// Gets box names for all boxes in the save file.
    /// </summary>
    /// <param name="sav"><see cref="SaveFile"/> that box names are being dumped for.</param>
    /// <returns>Returns default English box names in the event the save file does not have names (not exportable), or fails to return a box name.</returns>
    public static string[] GetBoxNames(SaveFile sav)
    {
        int count = sav.BoxCount;
        var result = new string[count];
        if (!sav.State.Exportable)
        {
            for (int i = 0; i < count; i++)
                result[i] = $"Box {i + 1}";
            return result;
        }

        for (int i = 0; i < count; i++)
        {
            try { result[i] = sav.GetBoxName(i); }
            catch { result[i] = $"Box {i + 1}"; }
        }

        return result;
    }
}
