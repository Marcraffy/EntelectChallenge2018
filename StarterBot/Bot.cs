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

        private IEnumerable<CellStateContainer> myAttackBuildings;
        private IEnumerable<CellStateContainer> myDefenceBuildings;
        private IEnumerable<CellStateContainer> myEnergyBuildings;
        private IEnumerable<CellStateContainer> myBuildings { get { return myAttackBuildings.Concat(myDefenceBuildings.Concat(myEnergyBuildings)); } }

        private IEnumerable<CellStateContainer> enemyAttackBuildings;
        private IEnumerable<CellStateContainer> enemyDefenceBuildings;
        private IEnumerable<CellStateContainer> enemyEnergyBuildings;
        private IEnumerable<CellStateContainer> enemyBuildings { get { return myAttackBuildings.Concat(myDefenceBuildings.Concat(myEnergyBuildings)); } }
        private IEnumerable<CellStateContainer> allBuildings { get { return myBuildings.Concat(enemyBuildings); } }

        public Bot(GameState gameState)
        {
            this.gameState = gameState;
            mapHeight = gameState.GameDetails.MapHeight;
            mapWidth = gameState.GameDetails.MapWidth;

            attackStats = gameState.GameDetails.BuildingsStats[BuildingType.Attack];
            defenseStats = gameState.GameDetails.BuildingsStats[BuildingType.Defense];
            energyStats = gameState.GameDetails.BuildingsStats[BuildingType.Energy];

            myAttackBuildings = GetBuildings(PlayerType.A, BuildingType.Attack);
            myDefenceBuildings = GetBuildings(PlayerType.A, BuildingType.Defense);
            myEnergyBuildings = GetBuildings(PlayerType.A, BuildingType.Energy);

            enemyAttackBuildings = GetBuildings(PlayerType.B, BuildingType.Attack);
            enemyDefenceBuildings = GetBuildings(PlayerType.B, BuildingType.Defense);
            enemyEnergyBuildings = GetBuildings(PlayerType.B, BuildingType.Energy);

            random = new Random((int) DateTime.Now.Ticks);

            player = gameState.Players.Single(x => x.PlayerType == PlayerType.A);
        }

        public string Run()
        {
            if (player.Energy < defenseStats.Price || player.Energy < energyStats.Price || player.Energy < attackStats.Price)
            {
                return "";
            }
            
            var rows = GetUndefendedEnemyBuildingRows();
            return !enemyAttackBuildings.Any() || rows.Count == 0 ? GetRandomCommand() : GetValidAttackCommand(rows[0]);
        }

        private string GetValidAttackCommand(int yCoordinate)
        {
            var xRandom = random.Next(mapWidth / 2);

            while (myBuildings.Any(x => x.X == xRandom && x.Y == yCoordinate && x.Buildings.Any()))
            {
                xRandom = random.Next(mapWidth / 2);
            }

            return $"{xRandom},{yCoordinate},{(int)BuildingType.Defense}";
        }

        private string GetRandomCommand()
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
        
        private List<int> GetUndefendedEnemyBuildingRows()
        {
            return enemyAttackBuildings.Select(enemyAttackBuilding => enemyAttackBuilding.Y).Where(y => !myDefenceBuildings.Any(myDefenseBuilding => myDefenseBuilding.Y == y)).ToList();
        }
        
        private List<CellStateContainer> GetBuildings(PlayerType playerType, BuildingType buildingType) 
        {
            return gameState.GameMap.SelectMany(cellStateContainers => 
                cellStateContainers.Where(cellStateContainer => 
                    cellStateContainer.CellOwner == playerType && cellStateContainer.Buildings.Any(x => x.BuildingType == buildingType))).ToList();
        }
    }
}