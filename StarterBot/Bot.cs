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

        private IEnumerable<CellStateContainer> enemyAttacks;

        private enum Phase
        {
            Attack,
            Defend,
            Save,
            Nop
        }

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
            enemyAttacks = GetEnemyAttacks();

            random = new Random((int) DateTime.Now.Ticks);

            player = gameState.Players.Single(x => x.PlayerType == PlayerType.A);
        }

        public string Run()
        {
            var phase = GetPhase();
            return GetCommand(phase);
        }

        private string GetCommand(Phase phase)
        {
            switch (phase)
            {
                case Phase.Attack:
                    {
                        return GetBestAttackCommand();
                    }
                case Phase.Defend:
                    {
                        return GetBestDefendCommand();
                    }
                case Phase.Save:
                    {
                        return GetBestSaveCommand();
                    }
                default:
                    {
                        return "";
                    }
            }
        }

        private string GetBestSaveCommand()
        {
            var yRandom = random.Next(mapHeight);

            while (myBuildings.Any(x => x.X == 0 && x.Y == yRandom && x.Buildings.Any()))
            {
                yRandom = random.Next(mapHeight);
            }

            return $"{0},{yRandom},{(int)BuildingType.Energy}";
        }

        private string GetBestDefendCommand()
        {
            var yCoordinate = myDefenceBuildings.Where(building => (building.X == 1 || building.X == 2) && !building.Buildings.Any()).Select(building => building.Y).First();
            if (!myBuildings.Any(x => x.X == 1 && x.Y == yCoordinate && x.Buildings.Any()))
            {
                return $"{1},{yCoordinate},{(int)BuildingType.Defense}";
            }
            else
            {
                return $"{2},{yCoordinate},{(int)BuildingType.Defense}";
            }
        }

        private string GetBestAttackCommand()
        {
            var xRandom = random.Next(mapWidth / 2);
            var yRandom = random.Next(mapHeight);

            while (myBuildings.Any(x => x.X == xRandom && x.Y == yRandom && x.Buildings.Any()))
            {
                xRandom = random.Next(mapWidth / 2);
                yRandom = random.Next(mapHeight);
            }

            return $"{xRandom},{yRandom},{(int)BuildingType.Attack}";
        }

        private Phase GetPhase()
        {
            if(player.Energy < attackStats.Price && player.Energy < defenseStats.Price && player.Energy < energyStats.Price )
            {
                return Phase.Nop;
            }
            if (enemyBuildings.Count() == 0)
            {
                if (enemyAttacks.Count() == 0)
                {
                    if (myBuildings.Count() == 0)
                    {
                        return Phase.Attack;
                    }
                    if (myDefenceBuildings.Count() < 3)
                    {
                        return Phase.Defend;
                    }

                    return Phase.Save;
                }

                return Phase.Defend;
            }

            if (enemyAttacks.Count() == 0)
            {
                if (myAttackBuildings.Count() == 3)
                {
                    return Phase.Defend;
                }

                return Phase.Attack;
            }

            return Phase.Defend;

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

        private List<int> GetIncommingAttackRows() => enemyAttackBuildings.Select(building => building.Y).Where(r => Enumerable.Range(0, 4).Any(x => gameState.GameMap[r][x].Missiles.Any())).ToList();

        private IEnumerable<CellStateContainer> GetEnemyAttacks()
        {
            return gameState.GameMap.SelectMany(cellStateContainers =>
                cellStateContainers.Where(cellStateContainer =>
                    cellStateContainer.Missiles.Any(missile => missile.PlayerType == PlayerType.B))).ToList();
        }


    }
}