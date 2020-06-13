﻿module TestHelpers

open MattEland.FSharpStarship.Logic.World

let standardGas = defaultGasses TileType.Floor

let makeFloorTile pos gasses = makeTileWithGasses TileType.Floor pos gasses
let makeWallTile pos = makeTileWithGasses TileType.Wall pos (defaultGasses TileType.Wall)
let makeSpaceTile pos = makeTileWithGasses TileType.Space pos (defaultGasses TileType.Space)
