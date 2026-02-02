using EasySave.Configuration;
using System;

public class TestPathProvider : IPathProvider
{
    private string statePath;

    // Chemin par défaut si aucun n'est fourni
    private const string DefaultStatePath = @"C:\Users\thais\Documents Local\FISA A3\Projet 3 c#\EasySave\state.json";

    // Constructeur optionnel
    public TestPathProvider(string statePath = null)
    {
        this.statePath = statePath ?? DefaultStatePath;
    }

    // Permet de récupérer le chemin du fichier JSON
    public string GetStatePath()
    {
        return statePath;
    }

    // Optionnel : permet de modifier le chemin à la main si besoin
    public void SetStatePath(string path)
    {
        statePath = path;
    }

    // Implémentations obligatoires de l'interface
    public string GetJobsConfigPath()
    {
        // Non utilisé dans ces tests
        return "unused_jobs.json";
    }

    public string GetDailyLogPath(DateTime date)
    {
        // Non utilisé dans ces tests
        return $"unused_log_{date:yyyyMMdd}.log";
    }
}
