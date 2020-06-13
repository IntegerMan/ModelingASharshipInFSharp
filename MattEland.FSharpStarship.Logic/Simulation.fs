﻿namespace MattEland.FSharpStarship.Logic

open World
open Positions

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

  let private shiftTopmostGas source dest world = 
    let gas = source |> getTopMostGas
    world
    |> replaceTile source.Pos (modifyTileGas gas -0.01M source)
    |> replaceTile dest.Pos (modifyTileGas gas 0.01M dest)

  let private tryFindLowPressureNeighbor tile world =
    getContext(world, tile) 
    |> getPresentNeighbors
    |> List.filter(fun n -> canGasFlowInto n && n.Pressure < tile.Pressure)
    |> List.sortBy(fun n -> n.Pressure) 
    |> List.tryHead

  let rec private equalizeTilePressure tile world =
    match world |> tryFindLowPressureNeighbor tile with
    | None -> world
    | Some neighbor ->
      let newWorld = world |> shiftTopmostGas tile neighbor
      let newTile = newWorld |> getTile tile.Pos

      // Call it again in case more can spill over
      equalizeTilePressure newTile newWorld

  let private simulateTileGas tile world =
    equalizeTilePressure tile world
    // TODO: Loop through pressurized gasses and swap them out

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

  let private simulateObjects tile world =
    world 
    |> getObjects tile.Pos 
    |> List.fold(fun newWorld obj -> newWorld |> simulateObject obj) world


  let simulateTile(tile: Tile, world: GameWorld): GameWorld = world |> simulateObjects tile |> simulateTileGas tile

  let simulate(world: GameWorld): GameWorld =
    world.Tiles 
    |> List.map(fun t -> t.Pos)
    |> List.distinct 
    |> List.fold(fun newWorld p -> simulateTile(getTile p newWorld, newWorld)) world