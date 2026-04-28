namespace Neptune.Models.DataTransferObjects
{
    public class ProjectLoadReducingResultDto
    {
        public int TreatmentBMPID { get; set; }
        public string TreatmentBMPName { get; set; }
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public double? EffectiveAreaAcres { get; set; }
        public double? DesignStormDepth85thPercentile { get; set; }
        public double? DesignVolume85thPercentile { get; set; }
        public double WetWeatherInflow { get; set; }
        public double WetWeatherTreated { get; set; }
        public double WetWeatherRetained { get; set; }
        public double WetWeatherUntreated { get; set; }
        public double WetWeatherTSSReduced { get; set; }
        public double WetWeatherTNReduced { get; set; }
        public double WetWeatherTPReduced { get; set; }
        public double WetWeatherFCReduced { get; set; }
        public double WetWeatherTCuReduced { get; set; }
        public double WetWeatherTPbReduced { get; set; }
        public double WetWeatherTZnReduced { get; set; }
        public double WetWeatherTSSInflow { get; set; }
        public double WetWeatherTNInflow { get; set; }
        public double WetWeatherTPInflow { get; set; }
        public double WetWeatherFCInflow { get; set; }
        public double WetWeatherTCuInflow { get; set; }
        public double WetWeatherTPbInflow { get; set; }
        public double WetWeatherTZnInflow { get; set; }
        public double SummerDryWeatherInflow { get; set; }
        public double SummerDryWeatherTreated { get; set; }
        public double SummerDryWeatherRetained { get; set; }
        public double SummerDryWeatherUntreated { get; set; }
        public double SummerDryWeatherTSSReduced { get; set; }
        public double SummerDryWeatherTNReduced { get; set; }
        public double SummerDryWeatherTPReduced { get; set; }
        public double SummerDryWeatherFCReduced { get; set; }
        public double SummerDryWeatherTCuReduced { get; set; }
        public double SummerDryWeatherTPbReduced { get; set; }
        public double SummerDryWeatherTZnReduced { get; set; }
        public double SummerDryWeatherTSSInflow { get; set; }
        public double SummerDryWeatherTNInflow { get; set; }
        public double SummerDryWeatherTPInflow { get; set; }
        public double SummerDryWeatherFCInflow { get; set; }
        public double SummerDryWeatherTCuInflow { get; set; }
        public double SummerDryWeatherTPbInflow { get; set; }
        public double SummerDryWeatherTZnInflow { get; set; }
        public double WinterDryWeatherInflow { get; set; }
        public double WinterDryWeatherTreated { get; set; }
        public double WinterDryWeatherRetained { get; set; }
        public double WinterDryWeatherUntreated { get; set; }
        public double WinterDryWeatherTSSReduced { get; set; }
        public double WinterDryWeatherTNReduced { get; set; }
        public double WinterDryWeatherTPReduced { get; set; }
        public double WinterDryWeatherFCReduced { get; set; }
        public double WinterDryWeatherTCuReduced { get; set; }
        public double WinterDryWeatherTPbReduced { get; set; }
        public double WinterDryWeatherTZnReduced { get; set; }
        public double WinterDryWeatherTSSInflow { get; set; }
        public double WinterDryWeatherTNInflow { get; set; }
        public double WinterDryWeatherTPInflow { get; set; }
        public double WinterDryWeatherFCInflow { get; set; }
        public double WinterDryWeatherTCuInflow { get; set; }
        public double WinterDryWeatherTPbInflow { get; set; }
        public double WinterDryWeatherTZnInflow { get; set; }
        public double DryWeatherInflow { get; set;}
        public double DryWeatherTreated { get; set;}
        public double DryWeatherRetained { get; set;}
        public double DryWeatherUntreated { get; set;}
        public double DryWeatherTSSReduced { get; set;}
        public double DryWeatherTNReduced { get; set;}
        public double DryWeatherTPReduced { get; set;}
        public double DryWeatherFCReduced { get; set;}
        public double DryWeatherTCuReduced { get; set;}
        public double DryWeatherTPbReduced { get; set;}
        public double DryWeatherTZnReduced { get; set;}
        public double DryWeatherTSSInflow { get; set;}
        public double DryWeatherTNInflow { get; set;}
        public double DryWeatherTPInflow { get; set;}
        public double DryWeatherFCInflow { get; set;}
        public double DryWeatherTCuInflow { get; set;}
        public double DryWeatherTPbInflow { get; set;}
        public double DryWeatherTZnInflow { get; set;}

        public double TotalInflow { get; set;}
        public double TotalTreated { get; set;}
        public double TotalRetained { get; set;}
        public double TotalUntreated { get; set;}
        public double TotalTSSReduced { get; set;}
        public double TotalTNReduced { get; set;}
        public double TotalTPReduced { get; set;}
        public double TotalFCReduced { get; set;}
        public double TotalTCuReduced { get; set;}
        public double TotalTPbReduced { get; set;}
        public double TotalTZnReduced { get; set;}
        public double TotalTSSInflow { get; set;}
        public double TotalTNInflow { get; set;}
        public double TotalTPInflow { get; set;}
        public double TotalFCInflow { get; set;}
        public double TotalTCuInflow { get; set;}
        public double TotalTPbInflow { get; set;}
        public double TotalTZnInflow { get; set;}

        public DateTime? LastCalculatedDate { get; set; }
    }
}