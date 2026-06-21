   // cart.js
document.addEventListener('DOMContentLoaded', () => {
    displayCart();
});

function displayCart() {
    let basket = JSON.parse(localStorage.getItem('courseBasket')) || [];
    let cartItems = document.getElementById('cartItems');
    let total = 0;

    cartItems.innerHTML = basket.map(item => {
        total += item.price * item.quantity;
        
        if(item.type == "Track"){
            
            item.name= item.name + " (this track contains " +item.numbers+ " courses)";
            
        }
        
        return `
            <tr class="cart-item">
                <td data-label="Description">${item.name}   </td>
                 <td data-label="Sub Total">${item.type} </td>
                <td data-label="Price"> ${item.price}</td>
                 <td data-label="Quantity">${item.quantity}</td>
                 <td data-label="Sub Total">${item.price} </td>
                 
                <td>
                    <button style="    padding: 10px 20px;
    background-color: #007BFF;
    color: white;
    border: none;
    cursor: pointer;" onclick="removeFromCart('${item.id}')" class="btn">Remove</button>
                </td>
            </tr>
        `;
    }).join('');

    document.getElementById('totalAmount').textContent = "Total: "+ total + " JOD";
}



displayCart();

function incrementQuantity(id) {
    let basket = JSON.parse(localStorage.getItem('courseBasket'));
    let found = basket.find(item => item.id === id);
    if (found) {
        found.quantity += 1;
        localStorage.setItem('courseBasket', JSON.stringify(basket));
        displayCart();
    }
}

function decrementQuantity(id) {
    let basket = JSON.parse(localStorage.getItem('courseBasket'));
    let found = basket.find(item => item.id === id);
    if (found && found.quantity > 1) {
        found.quantity -= 1;
        localStorage.setItem('courseBasket', JSON.stringify(basket));
        displayCart();
    }
}

function removeFromCart(id) {
    let basket = JSON.parse(localStorage.getItem('courseBasket'));
    basket = basket.filter(item => item.id !== id);
    localStorage.setItem('courseBasket', JSON.stringify(basket));
    displayCart();
}

function clearCart() {
    localStorage.removeItem('courseBasket');
    displayCart();
    updateCartUI();
}



