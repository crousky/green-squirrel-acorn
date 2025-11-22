const canvas = document.getElementById('gameCanvas');
const ctx = canvas.getContext('2d');

// Set canvas size
canvas.width = 800;
canvas.height = 600;

// Game State
const STATE = {
    MENU: 0,
    PLAYING: 1,
    GAME_OVER: 2,
    VICTORY: 3,
    LEVEL_TRANSITION: 4
};

let currentState = STATE.MENU;
let score = 0;
let level = 1;
let lives = 3;
let frames = 0;

// Assets
const assets = {
    player: new Image(),
    guac: new Image(),
    chip: new Image(),
};

assets.player.src = 'assets/player.png';
assets.guac.src = 'assets/guac.png';
assets.chip.src = 'assets/chip.png';

// Input Handling
const keys = {
    ArrowLeft: false,
    ArrowRight: false,
    Space: false
};

window.addEventListener('keydown', (e) => {
    if (e.code === 'ArrowLeft') keys.ArrowLeft = true;
    if (e.code === 'ArrowRight') keys.ArrowRight = true;
    if (e.code === 'Space') keys.Space = true;
});

window.addEventListener('keyup', (e) => {
    if (e.code === 'ArrowLeft') keys.ArrowLeft = false;
    if (e.code === 'ArrowRight') keys.ArrowRight = false;
    if (e.code === 'Space') keys.Space = false;
});

// Entities
class Player {
    constructor() {
        this.width = 50;
        this.height = 50;
        this.x = canvas.width / 2 - this.width / 2;
        this.y = canvas.height - 70;
        this.speed = 5;
        this.cooldown = 0;
    }

    update() {
        if (keys.ArrowLeft && this.x > 0) {
            this.x -= this.speed;
        }
        if (keys.ArrowRight && this.x + this.width < canvas.width) {
            this.x += this.speed;
        }

        if (keys.Space && this.cooldown <= 0) {
            this.shoot();
            this.cooldown = 20; // Frames between shots
        }

        if (this.cooldown > 0) this.cooldown--;
    }

    draw() {
        if (assets.player.complete && assets.player.naturalWidth !== 0) {
            ctx.drawImage(assets.player, this.x, this.y, this.width, this.height);
        } else {
            ctx.fillStyle = 'cyan';
            ctx.fillRect(this.x, this.y, this.width, this.height);
        }
    }

    shoot() {
        projectiles.push(new Projectile(this.x + this.width / 2 - 5, this.y, -10, 'player'));
    }
}

class Particle {
    constructor(x, y, color) {
        this.x = x;
        this.y = y;
        this.size = Math.random() * 5 + 2;
        this.speedX = (Math.random() - 0.5) * 10;
        this.speedY = (Math.random() - 0.5) * 10;
        this.color = color;
        this.life = 30; // Frames
    }

    update() {
        this.x += this.speedX;
        this.y += this.speedY;
        this.life--;
        this.size *= 0.9;
    }

    draw() {
        ctx.fillStyle = this.color;
        ctx.fillRect(this.x, this.y, this.size, this.size);
    }
}

class GuacBoss {
    constructor() {
        this.stages = 6;
        this.currentStage = 0; // 0 to 5

        // Health per stage increases with level
        this.hitsPerStage = 5 + level * 2;
        this.maxHealth = this.hitsPerStage * this.stages;
        this.currentHealth = this.maxHealth;

        this.baseSize = 200; // Starting diameter
        this.size = this.baseSize;

        this.x = canvas.width / 2;
        this.y = 150;

        this.dx = 2 + (level * 0.5);
        this.dy = 0;
        this.direction = 1;

        this.painTimer = 0; // For pain expression

        this.init();
    }

    init() {
        this.updateSize();
    }

    updateSize() {
        // Size shrinks as stages progress
        // Stage 0: 100%, Stage 5: ~20%
        const scale = 1 - (this.currentStage * 0.15);
        this.size = this.baseSize * scale;
    }

    takeDamage() {
        this.currentHealth--;
        this.painTimer = 10; // Show pain for 10 frames

        // Spawn particles
        for (let i = 0; i < 5; i++) {
            particles.push(new Particle(
                this.x + (Math.random() - 0.5) * this.size,
                this.y + (Math.random() - 0.5) * this.size,
                '#76c442'
            ));
        }

        // Check stage transition
        const healthPerStage = this.maxHealth / this.stages;
        const newStage = Math.floor((this.maxHealth - this.currentHealth) / healthPerStage);

        if (newStage > this.currentStage && newStage < this.stages) {
            this.currentStage = newStage;
            this.updateSize();
            // Explosion effect on shrink
            for (let i = 0; i < 20; i++) {
                particles.push(new Particle(this.x, this.y, '#5da035'));
            }
        }

        updateUI();
    }

    update() {
        if (this.currentHealth <= 0) return;

        // Wall collision
        if ((this.x + this.size / 2 >= canvas.width && this.direction === 1) ||
            (this.x - this.size / 2 <= 0 && this.direction === -1)) {
            this.direction *= -1;
            this.y += 20;
        }

        this.x += this.dx * this.direction;

        if (this.painTimer > 0) this.painTimer--;

        // Random shooting
        // Reduced frequency by 50%: 0.005 base (was 0.01)
        if (Math.random() < 0.005 + (level * 0.0025)) {
            this.shoot();
        }

        // Random Margarita Drop (Rare)
        if (Math.random() < 0.002) { // 0.2% chance per frame
            margaritas.push(new Margarita(this.x, this.y));
        }
    }

    draw() {
        if (this.currentHealth <= 0) return;

        ctx.save();
        ctx.translate(this.x, this.y);

        // Draw Blob Body
        ctx.fillStyle = '#76c442';
        ctx.beginPath();
        // Lumpy circle
        const radius = this.size / 2;
        for (let i = 0; i <= Math.PI * 2; i += 0.1) {
            const r = radius + Math.sin(i * 5 + frames * 0.1) * 5; // Animated lumps
            const x = Math.cos(i) * r;
            const y = Math.sin(i) * r;
            if (i === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
        }
        ctx.closePath();
        ctx.fill();

        // Texture
        ctx.fillStyle = 'rgba(0,0,0,0.1)';
        ctx.beginPath();
        ctx.arc(-radius / 3, -radius / 3, radius / 4, 0, Math.PI * 2);
        ctx.arc(radius / 3, radius / 4, radius / 5, 0, Math.PI * 2);
        ctx.fill();

        // Face
        this.drawFace(radius);

        ctx.restore();
    }

    drawFace(radius) {
        ctx.fillStyle = '#000';

        if (this.painTimer > 0) {
            // Pain Expression
            // X eyes
            this.drawXEye(-radius / 3, -radius / 4, radius / 5);
            this.drawXEye(radius / 3, -radius / 4, radius / 5);

            // Open mouth
            ctx.beginPath();
            ctx.arc(0, radius / 3, radius / 4, 0, Math.PI * 2);
            ctx.fill();
        } else {
            // Normal Expression
            // Eyes
            ctx.beginPath();
            ctx.arc(-radius / 3, -radius / 4, radius / 8, 0, Math.PI * 2);
            ctx.arc(radius / 3, -radius / 4, radius / 8, 0, Math.PI * 2);
            ctx.fill();

            // Angry Eyebrows
            ctx.lineWidth = 3;
            ctx.beginPath();
            ctx.moveTo(-radius / 2, -radius / 2);
            ctx.lineTo(-radius / 6, -radius / 3);
            ctx.moveTo(radius / 2, -radius / 2);
            ctx.lineTo(radius / 6, -radius / 3);
            ctx.stroke();

            // Mouth
            ctx.beginPath();
            ctx.arc(0, radius / 3, radius / 4, Math.PI, 0); // Frown
            ctx.stroke();
        }
    }

    drawXEye(x, y, size) {
        ctx.lineWidth = 3;
        ctx.beginPath();
        ctx.moveTo(x - size, y - size);
        ctx.lineTo(x + size, y + size);
        ctx.moveTo(x + size, y - size);
        ctx.lineTo(x - size, y + size);
        ctx.stroke();
    }

    shoot() {
        const types = ['jalapeno', 'lime', 'tomato'];
        const type = types[Math.floor(Math.random() * types.length)];
        // Reduced speed: 3 base (was 5)
        projectiles.push(new Projectile(this.x, this.y + this.size / 2, 3 + (level * 0.5), 'enemy', type));
    }

    isDead() {
        return this.currentHealth <= 0;
    }
}

class Projectile {
    constructor(x, y, speed, owner, type = 'chip') {
        this.x = x;
        this.y = y;
        this.width = 10;
        this.height = 20;
        this.speed = speed;
        this.owner = owner;
        this.type = type;
        this.markedForDeletion = false;
        this.rotation = 0; // For chips

        if (type !== 'chip') {
            this.width = 20;
            this.height = 20;
        }
    }

    update() {
        this.y += this.speed;
        if (this.type === 'chip') {
            this.rotation += 0.2;
        }
        if (this.y < 0 || this.y > canvas.height) {
            this.markedForDeletion = true;
        }
    }

    draw() {
        if (this.type === 'chip' && assets.chip.complete && assets.chip.naturalWidth !== 0) {
            ctx.save();
            ctx.translate(this.x + 10, this.y + 10);
            ctx.rotate(this.rotation);
            ctx.drawImage(assets.chip, -10, -10, 20, 20);
            ctx.restore();
        } else {
            ctx.beginPath();
            if (this.type === 'chip') {
                ctx.fillStyle = 'yellow';
                ctx.moveTo(this.x, this.y + this.height);
                ctx.lineTo(this.x + this.width / 2, this.y);
                ctx.lineTo(this.x + this.width, this.y + this.height);
                ctx.fill();
            } else if (this.type === 'jalapeno') {
                ctx.fillStyle = '#27ae60'; // Jalapeno Green
                // Draw a pepper shape
                ctx.moveTo(this.x + 10, this.y);
                ctx.quadraticCurveTo(this.x + 20, this.y + 5, this.x + 10, this.y + 20);
                ctx.quadraticCurveTo(this.x, this.y + 5, this.x + 10, this.y);
                ctx.fill();
            } else if (this.type === 'lime') {
                ctx.fillStyle = '#c0ff00'; // Lime Green
                ctx.arc(this.x + 10, this.y + 10, 8, 0, Math.PI * 2);
                ctx.fill();
                ctx.strokeStyle = '#fff';
                ctx.lineWidth = 1;
                ctx.stroke();
            } else if (this.type === 'tomato') {
                ctx.fillStyle = '#e74c3c'; // Tomato Red
                ctx.arc(this.x + 10, this.y + 10, 10, 0, Math.PI * 2);
                ctx.fill();
                // Green stem
                ctx.fillStyle = 'green';
                ctx.fillRect(this.x + 8, this.y - 2, 4, 4);
            }
        }
    }
}

class Margarita {
    constructor(x, y) {
        this.x = x;
        this.y = y;
        this.width = 30;
        this.height = 30;
        this.speed = 3;
        this.markedForDeletion = false;
    }

    update() {
        this.y += this.speed;
        if (this.y > canvas.height) {
            this.markedForDeletion = true;
        }
    }

    draw() {
        ctx.save();
        ctx.translate(this.x, this.y);

        // Glass (Triangle)
        ctx.fillStyle = 'rgba(255, 255, 255, 0.5)';
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.lineTo(30, 0);
        ctx.lineTo(15, 20);
        ctx.closePath();
        ctx.fill();
        ctx.strokeStyle = '#fff';
        ctx.stroke();

        // Liquid (Green)
        ctx.fillStyle = '#bada55';
        ctx.beginPath();
        ctx.moveTo(2, 2);
        ctx.lineTo(28, 2);
        ctx.lineTo(15, 18);
        ctx.closePath();
        ctx.fill();

        // Stem
        ctx.strokeStyle = '#fff';
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(15, 20);
        ctx.lineTo(15, 35);
        ctx.stroke();

        // Base
        ctx.beginPath();
        ctx.moveTo(10, 35);
        ctx.lineTo(20, 35);
        ctx.stroke();

        // Lime Wedge
        ctx.fillStyle = '#0f0';
        ctx.beginPath();
        ctx.arc(30, 0, 5, 0, Math.PI, true);
        ctx.fill();

        ctx.restore();
    }
}

class FloatingText {
    constructor(x, y, text, color) {
        this.x = x;
        this.y = y;
        this.text = text;
        this.color = color;
        this.life = 60;
        this.dy = -1;
    }

    update() {
        this.y += this.dy;
        this.life--;
    }

    draw() {
        ctx.fillStyle = this.color;
        ctx.font = '20px "Press Start 2P"';
        ctx.fillText(this.text, this.x, this.y);
    }
}

// Global Variables
let player;
let guacBoss;
let projectiles = [];
let particles = [];
let confetti = [];
let margaritas = [];
let floatingTexts = [];

function initGame() {
    player = new Player();
    guacBoss = new GuacBoss();
    projectiles = [];
    particles = [];
    confetti = [];
    margaritas = [];
    floatingTexts = [];

    // Generate static confetti
    for (let i = 0; i < 100; i++) {
        confetti.push({
            x: Math.random() * canvas.width,
            y: Math.random() * canvas.height,
            color: `hsl(${Math.random() * 360}, 70%, 50%)`,
            size: Math.random() * 3 + 1
        });
    }

    score = 0;
    level = 1;
    lives = 3;
    updateUI();
    currentState = STATE.PLAYING;
    document.getElementById('start-screen').classList.add('hidden');
    document.getElementById('game-over-screen').classList.add('hidden');
    document.getElementById('victory-screen').classList.add('hidden');
    loop();
}

function nextLevel() {
    level++;
    if (level > 10) {
        currentState = STATE.VICTORY;
        document.getElementById('victory-score').innerText = `Score: ${score}`;
        document.getElementById('victory-screen').classList.remove('hidden');
        return;
    }

    projectiles = [];
    particles = [];
    margaritas = [];
    floatingTexts = [];
    guacBoss = new GuacBoss();
    player.x = canvas.width / 2 - player.width / 2;
    updateUI();
}

function updateUI() {
    document.getElementById('score').innerText = `Score: ${score}`;
    document.getElementById('level').innerText = `Level: ${level}`;
    document.getElementById('lives').innerText = `Lives: ${lives}`;

    // Update Boss Health Bar
    if (guacBoss) {
        const pct = (guacBoss.currentHealth / guacBoss.maxHealth) * 100;
        document.getElementById('boss-health-fill').style.width = `${pct}%`;
    }
}

function checkCollisions() {
    projectiles.forEach(p => {
        if (p.markedForDeletion) return;

        if (p.owner === 'player') {
            // Check vs Boss (Circle collision)
            const dx = p.x - guacBoss.x;
            const dy = p.y - guacBoss.y;
            const distance = Math.sqrt(dx * dx + dy * dy);

            if (distance < guacBoss.size / 2) {
                guacBoss.takeDamage();
                p.markedForDeletion = true;
                score += 10;
                updateUI();
            }
        } else {
            // Check vs Player
            if (p.x < player.x + player.width &&
                p.x + p.width > player.x &&
                p.y < player.y + player.height &&
                p.y + p.height > player.y) {
                p.markedForDeletion = true;
                lives--;
                updateUI();
                if (lives <= 0) {
                    currentState = STATE.GAME_OVER;
                    document.getElementById('final-score').innerText = `Score: ${score}`;
                    document.getElementById('game-over-screen').classList.remove('hidden');
                }
            }
        }
    });

    // Check Margarita Collection
    margaritas.forEach(m => {
        if (m.markedForDeletion) return;

        if (m.x < player.x + player.width &&
            m.x + m.width > player.x &&
            m.y < player.y + player.height &&
            m.y + m.height > player.y) {
            m.markedForDeletion = true;
            lives++;
            updateUI();

            // Celebration
            floatingTexts.push(new FloatingText(player.x, player.y - 20, "SPICY! +1 LIFE", "#f1c40f"));

            for (let i = 0; i < 15; i++) {
                particles.push(new Particle(player.x + player.width / 2, player.y, `hsl(${Math.random() * 60 + 30}, 100%, 50%)`)); // Yellow/Orange particles
            }
        }
    });
}

function drawBackground() {
    ctx.fillStyle = '#050510'; // Deep space black
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // Draw stars
    ctx.fillStyle = '#fff';
    for (let i = 0; i < 50; i++) {
        const x = (Math.sin(i * 132.1 + 1) * 43758.5453 % 1) * canvas.width;
        const y = (Math.cos(i * 453.2 + 2) * 23421.345 % 1) * canvas.height;
        ctx.fillRect(Math.abs(x), Math.abs(y), 2, 2);
    }

    // Draw Confetti
    confetti.forEach(c => {
        ctx.fillStyle = c.color;
        ctx.fillRect(c.x, c.y, c.size, c.size);
        // Move confetti slowly
        c.y += 0.5;
        if (c.y > canvas.height) c.y = 0;
    });
}

function loop() {
    if (currentState !== STATE.PLAYING) return;
    frames++;

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    drawBackground();

    player.update();
    player.draw();

    guacBoss.update();
    guacBoss.draw();

    projectiles.forEach(p => p.update());
    projectiles.forEach(p => p.draw());
    projectiles = projectiles.filter(p => !p.markedForDeletion);

    particles.forEach(p => p.update());
    particles.forEach(p => p.draw());
    particles = particles.filter(p => p.life > 0);

    margaritas.forEach(m => m.update());
    margaritas.forEach(m => m.draw());
    margaritas = margaritas.filter(m => !m.markedForDeletion);

    floatingTexts.forEach(t => t.update());
    floatingTexts.forEach(t => t.draw());
    floatingTexts = floatingTexts.filter(t => t.life > 0);

    checkCollisions();

    if (guacBoss.isDead()) {
        nextLevel();
    }

    requestAnimationFrame(loop);
}

// Event Listeners for Buttons
document.getElementById('start-btn').addEventListener('click', initGame);
document.getElementById('restart-btn').addEventListener('click', initGame);
document.getElementById('play-again-btn').addEventListener('click', initGame);
