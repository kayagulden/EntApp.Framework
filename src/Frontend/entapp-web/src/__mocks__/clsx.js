export function clsx(...inputs) {
  return inputs
    .flat(Infinity)
    .filter((x) => typeof x === "string" && x.trim() !== "")
    .join(" ");
}

export default clsx;
