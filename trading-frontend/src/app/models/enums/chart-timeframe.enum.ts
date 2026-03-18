export enum ChartTimeframe {
  OneMonth = 'OneMonth',
  OneDay = 'OneDay',
  OneHour = 'OneHour',
  ThirtyMinutes = 'ThirtyMinutes',
  FifteenMinutes = 'FifteenMinutes'
}

export function getCandlePriority(candleValue: ChartTimeframe): number {
  switch (candleValue) {
    case ChartTimeframe.OneMonth: return 5;
    case ChartTimeframe.OneDay: return 4;
    case ChartTimeframe.OneHour: return 3;
    case ChartTimeframe.ThirtyMinutes: return 2;
    case ChartTimeframe.FifteenMinutes: return 1;
    default: return 4;
  }
}