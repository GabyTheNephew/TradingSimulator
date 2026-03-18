export enum ChartRange {
    OneYear = 'OneYear',
    OneMonth = 'OneMonth',
    OneDay = 'OneDay',
    OneHour = 'OneHour',
    ThirtyMinutes = 'ThirtyMinutes',
    FifteenMinutes = 'FifteenMinutes'
}

export function getRangePriority(range: ChartRange): number {
  switch (range) {
    case ChartRange.OneYear: return 6;
    case ChartRange.OneMonth: return 5;
    case ChartRange.OneDay: return 4;
    case ChartRange.OneHour: return 3;
    case ChartRange.ThirtyMinutes: return 2;
    case ChartRange.FifteenMinutes: return 1;
    default: return 5;
  }
}