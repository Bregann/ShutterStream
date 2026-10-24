/* eslint-disable no-unused-vars */
// Photo size enum matching C# PhotoSize
export enum PhotoSize {
  Small = 0,
  Large = 1,
  // Original resolution with the metadata stripped. Made on demand, so the first open can take a moment
  Full = 2
}
