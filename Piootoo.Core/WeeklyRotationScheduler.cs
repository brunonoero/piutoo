using Piootoo.Shared.Models;

namespace Piootoo.Core;

/// <summary>
/// Gestisce la schedulazione automatica della rotazione settimanale
/// </summary>
public class WeeklyRotationScheduler
{
    private readonly StrategyRotationManager _rotationManager;
    private readonly Dictionary<DateTime, WeeklySetup> _weeklySetups = new();
    private WeeklySetup? _currentSetup;

    public WeeklyRotationScheduler(StrategyRotationManager rotationManager)
    {
        _rotationManager = rotationManager;
    }

    /// <summary>
    /// Esegue la rotazione settimanale e produce il setup per la settimana
    /// Da chiamare ogni weekend
    /// </summary>
    public WeeklySetup ExecuteWeeklyRotation(DateTime weekendDate)
    {
        // Valuta le strategie basandosi sulle ultime N settimane
        var evaluation = _rotationManager.EvaluateAndRotateStrategies(weekendDate);

        var setup = new WeeklySetup
        {
            GenerationDate = weekendDate,
            Week = GetWeekNumber(weekendDate),
            Year = weekendDate.Year,
            StartDate = GetWeekStartDate(weekendDate),
            EndDate = GetWeekEndDate(weekendDate),
            EnabledStrategies = evaluation
                .Where(e => e.IsEnabled)
                .Select(e => e.StrategyName)
                .ToList(),
            StrategyEvaluations = evaluation,
            Configuration = _rotationManager.GetCurrentConfiguration()
        };

        // Salva il setup
        _weeklySetups[setup.StartDate] = setup;
        _currentSetup = setup;

        return setup;
    }

    /// <summary>
    /// Ottiene il setup per una data specifica (per backtesting)
    /// </summary>
    public WeeklySetup GetSetupForDate(DateTime date)
    {
        var weekStart = GetWeekStartDate(date);
        
        if (_weeklySetups.TryGetValue(weekStart, out var setup))
        {
            return setup;
        }

        // Se non esiste, genera il setup per quella settimana
        var previousWeekend = weekStart.AddDays(-1); // Domenica precedente
        return ExecuteWeeklyRotation(previousWeekend);
    }

    /// <summary>
    /// Ottiene il setup corrente (per real-time)
    /// </summary>
    public WeeklySetup GetCurrentSetup()
    {
        if (_currentSetup != null && IsSetupValid(_currentSetup))
        {
            return _currentSetup;
        }

        // Genera nuovo setup se necessario
        var now = DateTime.UtcNow;
        var lastWeekend = GetLastWeekend(now);
        return ExecuteWeeklyRotation(lastWeekend);
    }

    /// <summary>
    /// Verifica se una data è nel weekend (per triggering automatico)
    /// </summary>
    public bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }

    /// <summary>
    /// Verifica se è necessario eseguire una nuova rotazione
    /// </summary>
    public bool NeedsRotation(DateTime currentDate)
    {
        if (_currentSetup == null) return true;
        
        return currentDate > _currentSetup.EndDate;
    }

    /// <summary>
    /// Ottiene lo storico dei setup settimanali
    /// </summary>
    public List<WeeklySetup> GetSetupHistory(int? lastNWeeks = null)
    {
        var setups = _weeklySetups.Values.OrderByDescending(s => s.StartDate).ToList();
        
        if (lastNWeeks.HasValue)
        {
            return setups.Take(lastNWeeks.Value).ToList();
        }
        
        return setups;
    }

    private bool IsSetupValid(WeeklySetup setup)
    {
        // I confini del setup sono UTC come tutto il dominio: confrontarli con l'ora locale
        // farebbe scadere la settimana con ore di anticipo o di ritardo secondo l'host.
        var now = DateTime.UtcNow;
        return now >= setup.StartDate && now <= setup.EndDate;
    }

    private DateTime GetLastWeekend(DateTime date)
    {
        // Trova la domenica precedente o corrente
        var daysToSubtract = (int)date.DayOfWeek;
        if (daysToSubtract == 0) // È già domenica
            return date;
        
        return date.AddDays(-daysToSubtract);
    }

    private DateTime GetWeekStartDate(DateTime date)
    {
        // Lunedì della settimana
        var daysToSubtract = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
        if (daysToSubtract < 0) daysToSubtract += 7;
        
        return date.AddDays(-daysToSubtract).Date;
    }

    private DateTime GetWeekEndDate(DateTime date)
    {
        // Domenica della settimana
        var weekStart = GetWeekStartDate(date);
        return weekStart.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    private int GetWeekNumber(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date, 
            System.Globalization.CalendarWeekRule.FirstFourDayWeek, 
            DayOfWeek.Monday);
    }
}
