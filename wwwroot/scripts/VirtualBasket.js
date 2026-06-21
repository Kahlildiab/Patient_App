function addToCart(course) {
    let basket = JSON.parse(localStorage.getItem('courseBasket')) || [];
    let found = basket.find((item) => item.id === course.id);
    let foundtype = basket.find((item) => item.type === course.type);
    
    if (found) {
        document.getElementById(course.id).innerHTML="Already in Cart";
    } else {
        course.quantity = 1;
        basket.push(course);
        
    }
    localStorage.setItem('courseBasket', JSON.stringify(basket));
    updateCartUI();
}

function updateCartUI() {
    let basket = JSON.parse(localStorage.getItem('courseBasket')) || [];
    let cartIcon = document.getElementById("cartAmount");
    cartIcon.innerHTML = basket.reduce((acc, curr) => acc + curr.quantity, 0);

}


updateCartUI();

    $(document).ready(function () {
        // This function captures the password when the submit button is clicked and saves it to local storage
        
        localStorage.setItem("localpassword", localpassword); 
        
        $("button[type='submit']").click(function () {
            var localpassword = $("#localpassword").val(); // Get the password value
            
                localStorage.setItem("localpassword", localpassword); // Save password to local storage
                
                

            
        });
    });
