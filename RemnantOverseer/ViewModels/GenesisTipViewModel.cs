using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading;
using System.Threading.Tasks;
using RemnantOverseer.Services;

// Credit to the detectives on reddit and elsewhere that made this possible
// https://www.reddit.com/r/remnantgame/comments/1g4b01z/how_we_actually_solved_the_genesis_puzzle/

namespace RemnantOverseer.ViewModels;
public partial class GenesisTipViewModel: ViewModelBase, IDisposable
{
    private const int _cardID = 9358314;
    private const long _releaseDate = 638258940000000000; // new DateTime(2023, 07, 25, 15, 0, 0, DateTimeKind.Utc).Ticks
    private readonly int[] _offsets = [10, 0, 10, 0, 2, 8, 2, 8];

    private CancellationTokenSource _cancellationTokenSource { get; set; }

    [ObservableProperty]
    private int[] _currentGlyphs;

    [ObservableProperty]
    private int[] _requiredGlyphs;

#pragma warning disable CS8618
    public GenesisTipViewModel()
#pragma warning restore CS8618
    {
        _cancellationTokenSource = new CancellationTokenSource();
        SetSymbols();
        Task.Run(async () => { await WaitForUpdate(_cancellationTokenSource.Token); });
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
    }

    private void SetSymbols()
    {
        var current = DateTime.UtcNow.Ticks;
        var next = DateTime.UtcNow.AddHours(1).Ticks;
        CurrentGlyphs = GetCode(current);
        RequiredGlyphs = GetCode(next);
    }

    private int[] GetCode(long time)
    {
        var hoursSinceRelease = (int)Math.Floor((decimal)(time - _releaseDate) / ((long)3600 * 1000 * 10000));
        var offsetIndex = hoursSinceRelease % 8;
        int code = hoursSinceRelease * _cardID + _offsets[offsetIndex];
        var result = new int[4];
        for (int j = 3; j >= 0; j--)
        {
            var tmp = code % 10;
            result[j] = tmp < 0 ? tmp*-1 :tmp;
            code /= 10;
        }
        return result;
    }

    private async Task WaitForUpdate(CancellationToken cancellationToken)
    {
        var time = DateTime.Now.Minute*60 + DateTime.Now.Second;
        var span = TimeSpan.FromSeconds(3600 - time);
        try
        {
            Log.Instance.Information($"{nameof(GenesisTipViewModel)} started waiting for {span}");
            await Task.Delay(span, cancellationToken);
        }
        catch
        {
            Log.Instance.Information($"{nameof(GenesisTipViewModel)} cancelled waiting");
            return;
        }
        SetSymbols();
        _ = Task.Run(async () => { await WaitForUpdate(cancellationToken); });
    }
}
