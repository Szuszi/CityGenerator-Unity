using System;
using System.Collections.Generic;
using BlockGeneration;
using Services;
using Node = GraphModel.Node;

namespace BlockDivision
{
    class BlockDivider
    {
        private readonly List<Block> blocks;
        private readonly Random rand;
        public List<BoundingRectangle> BoundingRectangles { get; set; }
        
        private List<Block> Lots { get; set; }

        public BlockDivider(Random seededRandom, List<Block> blocksToDivide, List<Block> lots)
        {
            blocks = blocksToDivide;
            Lots = lots;
            BoundingRectangles = new List<BoundingRectangle>();
            rand = seededRandom;
        }

        public void DivideBlocks()
        {
            foreach (var block in blocks)
            {
                Lots.AddRange(DivideBlock(block, 1));
            }
        }
        
        public void SetBuildingHeights
        (
            float minBuildingHeight,
            float maxBuildingHeight,
            float baseHeight,
            int mapSize
        )
        {
            foreach (var lot in Lots)
            {
                var height = (float) rand.NextDouble() * maxBuildingHeight + minBuildingHeight;

                if (height > maxBuildingHeight / 2 && rand.Next(0, 10) != 2)
                {
                    height = height / 2;
                }

                if (mapSize - Math.Abs(lot.Nodes[0].X) < (float) 2 * mapSize / 3 
                    || mapSize - Math.Abs(lot.Nodes[0].Y) < (float) 2 * mapSize / 3)
                {
                    height = height / 2;
                }

                if (height < minBuildingHeight)
                {
                    height = height + minBuildingHeight;
                }
                    
                if (lot.Nodes.Count > 10)
                {
                    height = baseHeight;
                }

                lot.Height = height;
            }
        }

        private List<Block> DivideBlock(Block block,int iteration)
        {
            if (iteration > 6)
            {
                return new List<Block> {block};
            } 
            else if (block.Nodes.Count > 10)
            {
                return new List<Block> {block};
            }

            var minBoundingRect = BoundingService.GetMinBoundingRectangle(block);
            BoundingRectangles.Add(minBoundingRect);
            var cuttingLine = minBoundingRect.GetCutLine(rand);
            
            List<Block> newLots = SliceBlock(cuttingLine, block);
            
            //If the division is valid, try to divide even more, continue recursion
            if (newLots.Count > 1 && newLots.TrueForAll(ValidBlock))
            {
                var lots = new List<Block>();
                foreach (var newLot in newLots)
                {
                    lots.AddRange(DivideBlock(newLot, iteration + 1));
                }

                return lots;
            }

            //Else don't accept the division, return only the block, ends the recursion
            return new List<Block> {block};
        }

        private bool ValidBlock(Block block)
        {
            //Here we need to check if the newly created block is valid
            
            //Check if the size is not too small
            if (BoundingService.GetMinBoundingRectangle(block).GetArea() < 10f) return false;

            //Check if the aspect ratio is valid
            //Check etc.
            
            // ...
            return true;
        }

        private List<Block> SliceBlock(Line cutLine, Block blockToCut)
        {
            List<Block> resultList = new List<Block>();
            
            bool isStartingOnRight = cutLine.isNodeInRightSide(blockToCut.Nodes[0].GetNodeForm());
            bool currentlyOnRight = isStartingOnRight;
            
            int firstChangingNodeIdx = -1;
            var firstNewNode = new Node(0, 0);
            int lastChangingNodeIdx = -1;
            var lastNewNode = new Node(0,0);

            for (int i = 0; i < blockToCut.Nodes.Count; i++)
            {
                var currentIdx = i;
                var nextIdx = i == (blockToCut.Nodes.Count - 1) ? 0 : (i + 1);
                
                var currentNode = blockToCut.Nodes[currentIdx];
                var nextNode = blockToCut.Nodes[nextIdx];
                bool isNextNodeInRight = cutLine.isNodeInRightSide(nextNode.GetNodeForm());

                if (isNextNodeInRight != currentlyOnRight)
                {
                    Line lineToUseForIntersection = new Line(
                        currentNode.GetNodeForm(), nextNode.GetNodeForm()
                    );
                    
                    Node newNode = cutLine.getCrossing(lineToUseForIntersection);
                    
                    if (firstChangingNodeIdx == -1)
                    {
                        if (currentIdx == blockToCut.Nodes.Count - 1)
                        {
                            throw new InvalidOperationException(
                                "Line side change should have happened earlier"
                            );
                        }
                        
                        firstChangingNodeIdx = nextIdx;
                        firstNewNode = newNode;
                    }
                    else
                    {
                        var newBlock = new Block();

                        for (int x = lastChangingNodeIdx; x < (nextIdx == 0 ? currentIdx + 1 : nextIdx); x++) 
                        {
                            AddNewBlockNodeToBlock(newBlock, blockToCut.Nodes[x]);
                        }

                        AddNewBlockNodeToBlock(newBlock, new BlockNode(newNode.X, newNode.Y));
                        AddNewBlockNodeToBlock(newBlock, new BlockNode(lastNewNode.X, lastNewNode.Y));

                        resultList.Add(newBlock);
                    }

                    lastChangingNodeIdx = nextIdx;
                    lastNewNode = newNode;
                    currentlyOnRight = isNextNodeInRight;
                }
            }

            if (resultList.Count == 0)
            {
                /*
                Debug.Log("Division failed");
                resultList.Add(blockToCut);
                return resultList;
                */
                throw new InvalidOperationException("Slice failed to happen");
            }

            //Calculate last Slice
            Block lastNewBlock = new Block();

            if (lastChangingNodeIdx != 0)
            {
                for (int y = lastChangingNodeIdx; y < blockToCut.Nodes.Count; y++)
                {
                    AddNewBlockNodeToBlock(lastNewBlock, blockToCut.Nodes[y]);
                }
            }

            for (int y = 0; y < firstChangingNodeIdx; y++)
            {
                AddNewBlockNodeToBlock(lastNewBlock, blockToCut.Nodes[y]);
            }
            
            AddNewBlockNodeToBlock(lastNewBlock, new BlockNode(firstNewNode.X, firstNewNode.Y));
            AddNewBlockNodeToBlock(lastNewBlock, new BlockNode(lastNewNode.X, lastNewNode.Y));

            resultList.Add(lastNewBlock);

            //Remove BlockNodes which is too close to each other
            foreach (var block in resultList)
            {
                RemoveCloseBlockNodes(block);
            }
            
            return resultList;
        }

        private void RemoveCloseBlockNodes(Block block)
        {
            List<int> toBeRemovedIndexes = new List<int>(); 
                
            for (int i = 0; i < block.Nodes.Count; i++)
            {
                int nextIdx = i == block.Nodes.Count - 1 ? 0 : i + 1;
                
                BlockNode currentNode = block.Nodes[i];
                BlockNode nextNode = block.Nodes[nextIdx];

                if (currentNode.Equals(nextNode))
                {
                    toBeRemovedIndexes.Add(i);
                }
            }

            for(int i = toBeRemovedIndexes.Count-1; i >= 0; i--)
            {
                block.Nodes.RemoveAt(toBeRemovedIndexes[i]);
            }
        }

        private void AddNewBlockNodeToBlock(Block block, BlockNode blockToCopy)
        {
            var copiedNode = new BlockNode(blockToCopy.X, blockToCopy.Y);
            
            block.Nodes.Add(copiedNode);
            copiedNode.Block = block;
        }
    }
}