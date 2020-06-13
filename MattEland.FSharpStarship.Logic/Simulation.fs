﻿namespace MattEland.FSharpStarship.Logic

open World
open Positions
open Tiles
open Gasses
open TileGas
open GameObjects

module Simulations =

  type TileContext = 
    {
      Tile: Tile;
      Up: Tile option;
      Down: Tile option;
      Left: Tile option;
      Right: Tile option;
    }

  let getContext(world: GameWorld, tile: Tile): TileContext =
    {
      Tile=tile;
      Up=world |> tryGetTile (tile.Pos |> offset 0 -1);
      Down=world |> tryGetTile (tile.Pos |> offset 0 1);
      Left=world |> tryGetTile (tile.Pos |> offset -1 0);
      Right=world |> tryGetTile (tile.Pos |> offset 1 0);
    }

  let private getPotentialNeighbors context = [context.Up; context.Right; context.Down; context.Left]
  let private getPresentNeighbors context = context |> getPotentialNeighbors |> List.choose id

  let canGasFlowInto tile =
    match tile.TileType with
      | Floor | Space -> true
      | _ -> false

  let private shiftGas (source: Tile) (dest: Tile) gas world =
    world
    |> replaceTile source.Pos (modifyTileGas gas -0.01M source)
    |> replaceTile dest.Pos (modifyTileGas gas 0.01M dest)

  let private tryFindTargetForGasSpread gas pos world =
    let tile = world |> getTile pos
    let currentGas = getTileGas gas tile
    getContext(world, tile)
    |> getPresentNeighbors
    |> List.filter(fun n -> canGasFlowInto n && getTileGas gas n < currentGas)
    |> List.sortBy(fun n -> getTileGas gas n)
    |> List.tryHead

  let rec private equalizeTileGas pos gas world =
    let tile = world |> getTile pos
    let target = tryFindTargetForGasSpread gas pos world
    match target with
    | None -> world
    | Some neighbor ->
      world 
      |> shiftGas tile neighbor gas
      |> equalizeTileGas tile.Pos gas // May be more gas to shift

  let private simulateTileGas pos world = spreadableGasses |> List.fold(fun newWorld gas -> newWorld |> equalizeTileGas pos gas) world

  let humanOxygenIntake = 0.1M
  let scrubberCO2Intake = 0.1M
  
  let private convertTileGas amount gasSource gasGen tile =
    if tile |> getTileGas gasSource >= amount then
      tile |> modifyTileGas gasSource -amount |> modifyTileGas gasGen amount
    else
      tile

  let private simulatePerson (person: GameObject, world: GameWorld): GameWorld =
    let newTile = 
      world 
      |> getTile person.Pos 
      |> convertTileGas humanOxygenIntake Gas.Oxygen Gas.CarbonDioxide
    world |> replaceTile person.Pos newTile

  let private simulateAirScrubber (scrubber: GameObject, world: GameWorld): GameWorld =
    let newTile = 
      world 
      |> getTile scrubber.Pos 
      |> convertTileGas scrubberCO2Intake Gas.CarbonDioxide Gas.Oxygen
    world |> replaceTile scrubber.Pos newTile

  let private simulateObject obj world =
    match obj.ObjectType with
    | Astronaut -> simulatePerson(obj, world)
    | AirScrubber -> simulateAirScrubber(obj, world)

  let private simulateObjects pos world =
    world 
    |> getObjects pos
    |> List.fold(fun newWorld obj -> newWorld |> simulateObject obj) world

  let simulateTile(tile: Tile, world: GameWorld): GameWorld = world |> simulateObjects tile.Pos |> simulateTileGas tile.Pos

  let simulate(world: GameWorld): GameWorld =
    world.Tiles 
    |> List.map(fun t -> t.Pos)
    |> List.distinct 
    |> List.fold(fun newWorld p -> simulateTile(getTile p newWorld, newWorld)) world