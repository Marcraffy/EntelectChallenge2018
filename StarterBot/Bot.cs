using System;
using System.Collections.Generic;
using System.Linq;
using StarterBot.Entities;
using StarterBot.Enums;

namespace StarterBot
{
    public class Bot
    {
        private readonly GameState gameState;

        private readonly BuildingStats attackStats;
        private readonly BuildingStats defenseStats;
        private readonly BuildingStats energyStats;

        private readonly int mapWidth;
        private readonly int mapHeight;
        private readonly Player player;
        private readonly Random random;

        public Bot(GameState gameState)
        {
            this.gameState = gameState;
            mapHeight = gameState.GameDetails.MapHeight;
            mapWidth = gameState.GameDetails.MapWidth;

            attackStats = gameState.GameDetails.BuildingsStats[BuildingType.Attack];
            defenseStats = gameState.GameDetails.BuildingsStats[BuildingType.Defense];
            energyStats = gameState.GameDetails.BuildingsStats[BuildingType.Energy];

            random = new Random((int) DateTime.Now.Ticks);

            player = gameState.Players.Single(x => x.PlayerType == PlayerType.A);
        }

        public string Run()
        {
            if (player.Energy < defenseStats.Price || player.Energy < energyStats.Price || player.Energy < attackStats.Price)
            {
                return "";
            }
            
            var opponentAttackBuildings = GetBuildings(PlayerType.B, BuildingType.Attack);

            var myAttackBuildings = GetBuildings(PlayerType.A, BuildingType.Attack);
            var myDefenseBuildings = GetBuildings(PlayerType.A, BuildingType.Defense);

            var myBuildings = myAttackBuildings.Concat(myDefenseBuildings).ToList(); 
            
            var rows = GetUndefendedEnemyBuildingRows(opponentAttackBuildings, myDefenseBuildings);
            return !opponentAttackBuildings.Any() ? GetRandomCommand(myBuildings) : (rows.Count > 0 ? GetValidAttackCommand(rows[0], myBuildings) : GetRandomCommand(myBuildings));
        }

        private string GetValidAttackCommand(int yCoordinate, List<CellStateContainer> myBuildings)
        {
            var xRandom = random.Next(mapWidth / 2);

            while (myBuildings.Any(x => x.X == xRandom && x.Y == yCoordinate && x.Buildings.Any()))
            {
                xRandom = random.Next(mapWidth / 2);
            }

            return $"{xRandom},{yCoordinate},{(int)BuildingType.Defense}";
        }

        private string GetRandomCommand(List<CellStateContainer> myBuildings)
        {
            var xRandom = random.Next(mapWidth / 2);
            var yRandom = random.Next(mapHeight);
            var btRandom = random.Next(Enum.GetNames(typeof(BuildingType)).Length);

            while (myBuildings.Any(x => x.X == xRandom && x.Y == yRandom && x.Buildings.Any()))
            {
                xRandom = random.Next(mapWidth / 2);
                yRandom = random.Next(mapHeight);
            }

            return $"{xRandom},{yRandom},{btRandom}";
        }
        
        private List<int> GetUndefendedEnemyBuildingRows(List<CellStateContainer> opponentAttackBuildings,
            List<CellStateContainer> myDefenseBuildings)
        {
            return opponentAttackBuildings.Select(enemyAttackBuilding => enemyAttackBuilding.Y).Where(y => !myDefenseBuildings.Any(myDefenseBuilding => myDefenseBuilding.Y == y)).ToList();
        }
        
        private List<CellStateContainer> GetBuildings(PlayerType playerType, BuildingType buildingType) 
        {
            return gameState.GameMap.SelectMany(cellStateContainers => 
                cellStateContainers.Where(cellStateContainer => 
                    cellStateContainer.CellOwner == playerType && cellStateContainer.Buildings.Any(x => x.BuildingType == buildingType))).ToList();
        }
    }
}