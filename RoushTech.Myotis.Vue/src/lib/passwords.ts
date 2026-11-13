const WORDS = [
    "amber",
    "anchor",
    "basil",
    "beacon",
    "cedar",
    "cobalt",
    "delta",
    "ember",
    "fable",
    "falcon",
    "garnet",
    "harbor",
    "indigo",
    "juniper",
    "kettle",
    "lantern",
    "maple",
    "meadow",
    "nectar",
    "orbit",
    "pebble",
    "quartz",
    "raven",
    "saffron",
    "summit",
    "tundra",
    "velvet",
    "willow",
];

function pick(): string {
    const index = crypto.getRandomValues(new Uint32Array(1))[0] ?? 0;
    return WORDS[index % WORDS.length] ?? "amber";
}

export function suggestPassword(): string {
    const digits = (crypto.getRandomValues(new Uint32Array(1))[0] ?? 0) % 90;
    return `${pick()}-${pick()}-${digits + 10}-${pick()}`;
}
