const apiBase = "http://localhost:5055/api";

const state = {
    products: [],
    tables: [],
    orders: [],
    cart: []
};

const money = new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "EUR"
});

document.addEventListener("DOMContentLoaded", () => {
    document.getElementById("refreshButton").addEventListener("click", loadAll);
    document.getElementById("reloadProductsButton").addEventListener("click", loadAll);
    document.getElementById("newOrderButton").addEventListener("click", clearCart);
    document.getElementById("clearCartButton").addEventListener("click", clearCart);
    document.getElementById("sendOrderButton").addEventListener("click", sendOrder);
    document.getElementById("printButton").addEventListener("click", () => window.print());
    document.getElementById("tableSelect").addEventListener("change", renderCart);
    loadAll();
    setInterval(loadOrders, 5000);
});

async function loadAll() {
    try {
        await api("/health");
        const [products, tables, orders] = await Promise.all([
            api("/products"),
            api("/tables"),
            api("/orders")
        ]);
        state.products = products;
        state.tables = tables;
        state.orders = orders;
        renderProducts();
        renderTables();
        renderOrders();
        renderCart();
        renderStats();
    } catch (error) {
        showError(error.message);
    }
}

async function loadOrders() {
    try {
        state.orders = await api("/orders");
        renderOrders();
        renderStats();
    } catch (error) {
        console.warn(error);
    }
}

async function api(path, options = {}) {
    const response = await fetch(apiBase + path, {
        headers: { "Content-Type": "application/json" },
        ...options
    });
    const data = await response.json();
    if (!response.ok || data.error) {
        throw new Error(data.error || "Request failed");
    }
    return data;
}

function renderProducts() {
    const menuGrid = document.getElementById("menuGrid");
    const tableBody = document.getElementById("productsTableBody");

    if (!state.products.length) {
        menuGrid.innerHTML = `<div class="empty-state">No products found in the database.</div>`;
        tableBody.innerHTML = `<tr><td colspan="5">No products found.</td></tr>`;
        return;
    }

    menuGrid.innerHTML = state.products.map(product => `
        <button class="menu-item" data-product-id="${product.Id}">
            <span>${iconFor(product.CategoryName || product.Name)}</span>
            <strong>${escapeHtml(product.Name)}</strong>
            <small>${money.format(product.PriceWithVat)}</small>
        </button>
    `).join("");

    menuGrid.querySelectorAll(".menu-item").forEach(button => {
        button.addEventListener("click", () => addToCart(Number(button.dataset.productId)));
    });

    tableBody.innerHTML = state.products.map(product => `
        <tr>
            <td>${escapeHtml(product.Name)}</td>
            <td>${escapeHtml(product.CategoryName || "Uncategorized")}</td>
            <td>${money.format(product.Price)}</td>
            <td>${product.VAT}%</td>
            <td><span class="status ready">Active</span></td>
        </tr>
    `).join("");
}

function renderTables() {
    const select = document.getElementById("tableSelect");
    const grid = document.getElementById("tableGrid");
    const selectedValue = select.value;
    const busyIds = new Set(state.orders.map(order => order.TableId));
    const readyIds = new Set(state.orders.filter(order => order.Status === "Ready").map(order => order.TableId));

    select.innerHTML = `<option value="">Choose table</option>` + state.tables.map(table => `
        <option value="${table.Id}">${escapeHtml(table.Name)} - ${escapeHtml(table.HallName || "Hall")}</option>
    `).join("");
    select.value = selectedValue;

    grid.innerHTML = state.tables.map(table => {
        const status = readyIds.has(table.Id) ? "ready" : busyIds.has(table.Id) ? "busy" : "free";
        const label = status === "ready" ? "Ready" : status === "busy" ? "Busy" : "Free";
        return `<button class="table-card ${status}" data-table-id="${table.Id}">${escapeHtml(table.Name)}<span>${label}</span></button>`;
    }).join("");

    grid.querySelectorAll(".table-card").forEach(button => {
        button.addEventListener("click", () => {
            select.value = button.dataset.tableId;
            renderCart();
        });
    });
}

function renderOrders() {
    const list = document.getElementById("ordersList");
    if (!state.orders.length) {
        list.innerHTML = `<div class="empty-state">No active kitchen orders.</div>`;
        renderTables();
        return;
    }

    list.innerHTML = state.orders.map(order => `
        <article class="kitchen-card ${statusClass(order.Status)}">
            <div>
                <strong>Order #${order.Id}</strong>
                <span>${escapeHtml(order.TableName)} · ${escapeHtml(order.WaiterName)}</span>
            </div>
            <p>${order.Items.map(item => `${item.Quantity}x ${escapeHtml(item.ProductName)}`).join(", ")}</p>
            ${order.Items.some(item => item.Comment) ? `<small>Comment: ${escapeHtml(order.Items.map(item => item.Comment).filter(Boolean).join("; "))}</small>` : `<small>${escapeHtml(order.Status)}</small>`}
            <div class="kitchen-actions">
                <button data-status="Pending" data-order-id="${order.Id}">Pending</button>
                <button data-status="Preparing" data-order-id="${order.Id}">Preparing</button>
                <button data-status="Ready" data-order-id="${order.Id}">Ready</button>
            </div>
        </article>
    `).join("");

    list.querySelectorAll("[data-status]").forEach(button => {
        button.addEventListener("click", () => updateStatus(Number(button.dataset.orderId), button.dataset.status));
    });
    renderTables();
}

function renderCart() {
    const tableSelect = document.getElementById("tableSelect");
    const selectedOption = tableSelect.options[tableSelect.selectedIndex];
    const selectedTable = selectedOption && selectedOption.value ? selectedOption.textContent : "No table selected";
    const cartList = document.getElementById("cartList");
    const total = cartTotal();

    document.getElementById("selectedTableLabel").textContent = selectedTable;
    document.getElementById("cartTotal").textContent = money.format(total);

    if (!state.cart.length) {
        cartList.innerHTML = `<div class="empty-state">Click a product to add it here.</div>`;
        return;
    }

    cartList.innerHTML = state.cart.map(item => `
        <div class="order-row">
            <span>${item.quantity}x</span>
            <strong>${escapeHtml(item.name)}</strong>
            <em>${money.format(item.quantity * item.priceWithVat)}</em>
            <button class="tiny-button" data-remove-id="${item.productId}">×</button>
        </div>
    `).join("");

    cartList.querySelectorAll("[data-remove-id]").forEach(button => {
        button.addEventListener("click", () => removeFromCart(Number(button.dataset.removeId)));
    });
}

function renderStats() {
    document.getElementById("openOrdersCount").textContent = state.orders.length;
    document.getElementById("tablesCount").textContent = state.tables.length;
    document.getElementById("readyOrdersCount").textContent = state.orders.filter(order => order.Status === "Ready").length;
    document.getElementById("activeSales").textContent = money.format(state.orders.reduce((sum, order) => sum + Number(order.Total || 0), 0));
}

function addToCart(productId) {
    const product = state.products.find(item => item.Id === productId);
    if (!product) return;
    const existing = state.cart.find(item => item.productId === productId);
    if (existing) {
        existing.quantity += 1;
    } else {
        state.cart.push({
            productId: product.Id,
            name: product.Name,
            priceWithVat: product.PriceWithVat,
            quantity: 1
        });
    }
    renderCart();
}

function removeFromCart(productId) {
    const existing = state.cart.find(item => item.productId === productId);
    if (!existing) return;
    existing.quantity -= 1;
    if (existing.quantity <= 0) {
        state.cart = state.cart.filter(item => item.productId !== productId);
    }
    renderCart();
}

async function sendOrder() {
    const tableId = Number(document.getElementById("tableSelect").value);
    if (!tableId) {
        alert("Choose a table first.");
        return;
    }
    if (!state.cart.length) {
        alert("Add at least one product first.");
        return;
    }

    await api("/orders", {
        method: "POST",
        body: JSON.stringify({
            tableId,
            waiterId: 2,
            items: state.cart.map(item => ({
                productId: item.productId,
                quantity: item.quantity,
                comment: ""
            }))
        })
    });

    clearCart();
    await loadOrders();
    alert("Order sent to kitchen.");
}

async function updateStatus(orderId, status) {
    await api(`/orders/${orderId}/status`, {
        method: "PUT",
        body: JSON.stringify({ status })
    });
    await loadOrders();
}

function clearCart() {
    state.cart = [];
    renderCart();
}

function cartTotal() {
    return state.cart.reduce((sum, item) => sum + item.quantity * item.priceWithVat, 0);
}

function statusClass(status) {
    if (status === "Preparing") return "preparing";
    if (status === "Ready") return "ready";
    return "pending";
}

function iconFor(text) {
    const value = text.toLowerCase();
    if (value.includes("drink") || value.includes("water") || value.includes("cola")) return "🥤";
    if (value.includes("coffee")) return "☕";
    if (value.includes("pizza")) return "🍕";
    if (value.includes("salad")) return "🥗";
    if (value.includes("pasta")) return "🍝";
    return "🍔";
}

function showError(message) {
    document.getElementById("menuGrid").innerHTML = `<div class="empty-state error">Could not connect to backend: ${escapeHtml(message)}<br>Start the Windows Forms app, then press Refresh.</div>`;
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
