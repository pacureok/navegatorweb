// MinesweeperGame.js
document.addEventListener('DOMContentLoaded', () => {
    const gridElement = document.getElementById('minesweeper-grid');
    const messageElement = document.getElementById('message');
    const restartButton = document.getElementById('restart-button');

    const ROWS = 10;
    const COLS = 10;
    const MINES = 15; // Número de minas

    let board = [];
    let gameEnded = false;
    let minesLocated = 0; // Para la lógica de ganar

    // Inicializar el tablero de juego
    function initializeGame() {
        gameEnded = false;
        minesLocated = 0;
        messageElement.textContent = '';
        board = Array(ROWS).fill(0).map(() => Array(COLS).fill({
            isMine: false,
            isOpened: false,
            isFlagged: false,
            minesAround: 0
        }));

        placeMines();
        calculateMinesAround();
        renderGrid();
    }

    // Colocar las minas aleatoriamente
    function placeMines() {
        let minesPlaced = 0;
        while (minesPlaced < MINES) {
            const row = Math.floor(Math.random() * ROWS);
            const col = Math.floor(Math.random() * COLS);

            if (!board[row][col].isMine) {
                board[row][col] = { ...board[row][col], isMine: true };
                minesPlaced++;
            }
        }
    }

    // Calcular el número de minas alrededor de cada celda
    function calculateMinesAround() {
        for (let r = 0; r < ROWS; r++) {
            for (let c = 0; c < COLS; c++) {
                if (!board[r][c].isMine) {
                    let count = 0;
                    for (let i = -1; i <= 1; i++) {
                        for (let j = -1; j <= 1; j++) {
                            const newR = r + i;
                            const newC = c + j;

                            if (newR >= 0 && newR < ROWS && newC >= 0 && newC < COLS && board[newR][newC].isMine) {
                                count++;
                            }
                        }
                    }
                    board[r][c] = { ...board[r][c], minesAround: count };
                }
            }
        }
    }

    // Renderizar (dibujar) la cuadrícula en el HTML
    function renderGrid() {
        gridElement.innerHTML = '';
        gridElement.style.gridTemplateColumns = `repeat(${COLS}, 1fr)`;

        for (let r = 0; r < ROWS; r++) {
            for (let c = 0; c < COLS; c++) {
                const cellDiv = document.createElement('div');
                cellDiv.classList.add('cell');
                cellDiv.dataset.row = r;
                cellDiv.dataset.col = c;

                const cell = board[r][c];

                if (cell.isOpened) {
                    cellDiv.classList.add('opened');
                    if (cell.isMine) {
                        cellDiv.classList.add('mine');
                        cellDiv.textContent = '💣'; // Mina
                    } else if (cell.minesAround > 0) {
                        cellDiv.textContent = cell.minesAround;
                        cellDiv.classList.add(`number-${cell.minesAround}`);
                    }
                } else if (cell.isFlagged) {
                    cellDiv.classList.add('flagged');
                    cellDiv.textContent = '🚩'; // Bandera
                }

                cellDiv.addEventListener('click', () => openCell(r, c));
                cellDiv.addEventListener('contextmenu', (e) => {
                    e.preventDefault(); // Prevenir el menú contextual del navegador
                    toggleFlag(r, c);
                });

                gridElement.appendChild(cellDiv);
            }
        }
    }

    // Abrir una celda al hacer clic izquierdo
    function openCell(row, col) {
        if (gameEnded || board[row][col].isOpened || board[row][col].isFlagged) {
            return;
        }

        board[row][col] = { ...board[row][col], isOpened: true };

        if (board[row][col].isMine) {
            revealAllMines();
            messageElement.textContent = '¡Boom! Game Over. 💥';
            messageElement.style.color = 'red';
            gameEnded = true;
            return;
        }

        if (board[row][col].minesAround === 0) {
            // Abrir celdas adyacentes si no hay minas alrededor (recursividad)
            for (let i = -1; i <= 1; i++) {
                for (let j = -1; j <= 1; j++) {
                    const newR = row + i;
                    const newC = col + j;

                    if (newR >= 0 && newR < ROWS && newC >= 0 && newC < COLS && !board[newR][newC].isOpened) {
                        openCell(newR, newC); // Llamada recursiva
                    }
                }
            }
        }

        renderGrid();
        checkWin();
    }

    // Colocar/quitar bandera al hacer clic derecho
    function toggleFlag(row, col) {
        if (gameEnded || board[row][col].isOpened) {
            return;
        }

        board[row][col] = { ...board[row][col], isFlagged: !board[row][col].isFlagged };
        renderGrid();
        checkWin();
    }

    // Revelar todas las minas al perder
    function revealAllMines() {
        for (let r = 0; r < ROWS; r++) {
            for (let c = 0; c < COLS; c++) {
                if (board[r][c].isMine) {
                    board[r][c] = { ...board[r][c], isOpened: true };
                }
            }
        }
        renderGrid();
    }

    // Comprobar si el jugador ha ganado
    function checkWin() {
        let openedCells = 0;
        let correctFlags = 0;

        for (let r = 0; r < ROWS; r++) {
            for (let c = 0; c < COLS; c++) {
                const cell = board[r][c];
                if (cell.isOpened && !cell.isMine) {
                    openedCells++;
                }
                if (cell.isFlagged && cell.isMine) {
                    correctFlags++;
                }
            }
        }

        // Condición de victoria: todas las celdas no minadas están abiertas
        // O todas las minas están correctamente marcadas con banderas
        if (openedCells === (ROWS * COLS) - MINES) {
            messageElement.textContent = '¡Felicidades! ¡Has ganado! 🎉';
            messageElement.style.color = 'green';
            gameEnded = true;
            revealAllMines(); // Opcional: mostrar las minas restantes
        } else if (correctFlags === MINES && openedCells < (ROWS * COLS) - MINES) {
            // Una condición adicional para ganar si todas las minas están marcadas,
            // incluso si no todas las celdas seguras están abiertas (algunas implementaciones lo tienen)
            // Por simplicidad, nos quedaremos con la primera condición.
        }
    }

    // Botón de reiniciar
    restartButton.addEventListener('click', initializeGame);

    // Iniciar el juego al cargar la página
    initializeGame();
});
