const units: [Intl.RelativeTimeFormatUnit, number][] = [
    ["day", 86_400_000],
    ["hour", 3_600_000],
    ["minute", 60_000],
];

export function relative(iso: string): string {
    const diff = new Date(iso).getTime() - Date.now();
    const format = new Intl.RelativeTimeFormat(undefined, { numeric: "auto" });
    for (const [unit, ms] of units) {
        if (Math.abs(diff) >= ms) {
            return format.format(Math.round(diff / ms), unit);
        }
    }
    return format.format(0, "minute");
}
