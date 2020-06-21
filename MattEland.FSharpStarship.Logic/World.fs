﻿namespace MattEland.FSharpStarship.Logic

open Positions
open Gasses
open Tiles
open TileGas

module World =

  type GameWorld = 
    {
      Tiles: List<Tile>
    }

  let getTile pos tiles = tiles |> List.find(fun t -> t.Pos = pos)
  let tryGetTile pos tiles = tiles |> List.tryFind(fun t -> t.Pos = pos)

  let getGasByPos (tiles: List<Tile>, pos: Pos, gas: Gas): decimal = tiles |> getTile pos |> getTileGas gas

  let makeTile flags objects tileArt pos = 
    let gasses = defaultGasses
    {
      Flags=flags
      Pos=pos
      Gasses=gasses
      Pressure=gasses |> calculatePressure
      Art=tileArt
      Objects=objects
    }
   
  let makeTileWithGasses flags pos objects gasses = 
    let tile = makeTile flags objects [] pos
    {tile with Gasses=gasses; Pressure=gasses |> calculatePressure}

  let private replaceTileIfMatch(tile: Tile, testPos: Pos, newTile: Tile): Tile =
    if tile.Pos = testPos then
      newTile
    else
      tile

  let replaceTile newTile tiles = tiles |> List.map(fun t -> replaceTileIfMatch(t, newTile.Pos, newTile))

  let create tiles = {Tiles=tiles}